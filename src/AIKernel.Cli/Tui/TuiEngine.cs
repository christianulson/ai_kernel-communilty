using System.Net.Http.Json;
using Spectre.Console;

namespace AIKernel.Cli.Tui;

public sealed class TuiEngine
{
    private readonly TuiChatPanel _chat = new();
    private readonly TuiStatusPanel _status = new();
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private bool _running;

    private static readonly Dictionary<string, string> SlashCommands = new()
    {
        ["/help"] = "Mostra esta ajuda",
        ["/explain"] = "Explica código (cole o código após o comando)",
        ["/fix"] = "Tenta corrigir código com problemas",
        ["/status"] = "Mostra status detalhado do kernel",
        ["/clear"] = "Limpa o chat",
        ["/connect"] = "Conecta ao backend (URL opcional)",
        ["/exit"] = "Sai do modo interativo",
        ["/quit"] = "Sai do modo interativo",
    };

    public TuiEngine(string baseUrl = "http://localhost:5000")
    {
        _baseUrl = baseUrl;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _running = true;
        _chat.AddMessage("system", "AI Kernel TUI iniciado. Digite /help para comandos.");

        await CheckHealthAsync(ct);

        while (_running && !ct.IsCancellationRequested)
        {
            RenderLayout();

            var input = Console.ReadLine() ?? "";

            if (ct.IsCancellationRequested) break;

            if (string.IsNullOrWhiteSpace(input))
                continue;

            await ProcessInputAsync(input, ct);
        }

        _chat.AddMessage("system", "TUI encerrado.");
        RenderLayout();
    }

    private void RenderLayout()
    {
        AnsiConsole.Clear();

        var chatPanel = _chat.Render();
        var statusPanel = _status.Render();

        var layout = new Grid()
            .AddColumn(new GridColumn().PadRight(2))
            .AddColumn();

        layout.AddRow(chatPanel, statusPanel);

        AnsiConsole.Write(layout);

        AnsiConsole.MarkupLine("[grey]─[/]" .PadRight(110, '─'));
        AnsiConsole.Markup("[bold cyan]> [/]");
    }

    private async Task ProcessInputAsync(string input, CancellationToken ct)
    {
        var trimmed = input.Trim();

        if (trimmed.StartsWith('/'))
        {
            var spaceIdx = trimmed.IndexOf(' ');
            var command = spaceIdx > 0 ? trimmed[..spaceIdx] : trimmed;
            var args = spaceIdx > 0 ? trimmed[(spaceIdx + 1)..] : "";

            await HandleSlashCommandAsync(command, args, ct);
        }
        else
        {
            await SendMessageAsync(trimmed, ct);
        }

        await CheckHealthAsync(ct);
    }

    private async Task HandleSlashCommandAsync(string command, string args, CancellationToken ct)
    {
        switch (command.ToLowerInvariant())
        {
            case "/help":
                _chat.AddMessage("system", "Comandos disponíveis:");
                foreach (var (cmd, desc) in SlashCommands)
                    _chat.AddMessage("system", $"  {cmd,-20} {desc}");
                break;

            case "/status":
                _status.LastAction = "status check";
                await CheckHealthAsync(ct);
                _chat.AddMessage("system", $"Status: {_status.Status} | Risco: {_status.RiskLevel} | Modo: {_status.Mode}");
                break;

            case "/clear":
                _chat.Clear();
                _chat.AddMessage("system", "Chat limpo.");
                break;

            case "/connect":
                if (!string.IsNullOrWhiteSpace(args))
                {
                    _http.BaseAddress = new Uri(args);
                    _chat.AddMessage("system", $"Tentando conectar em: {args}");
                }
                await CheckHealthAsync(ct);
                break;

            case "/exit":
            case "/quit":
                _running = false;
                break;

            case "/explain":
            case "/fix":
                if (string.IsNullOrWhiteSpace(args))
                {
                    _chat.AddMessage("error", $"Uso: {command} <código>. Cole o código após o comando.");
                    return;
                }
                _status.Status = "Processando...";
                RenderLayout();
                await SendToBackendAsync(command, args, ct);
                break;

            default:
                _chat.AddMessage("error", $"Comando desconhecido: {command}. Digite /help para ajuda.");
                break;
        }
    }

    private async Task SendMessageAsync(string message, CancellationToken ct)
    {
        _chat.AddMessage("user", message);
        _status.Status = "Processando...";
        RenderLayout();

        try
        {
            var response = await _http.PostAsJsonAsync("/agent/run",
                new { prompt = message, mode = "gateway" }, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AgentRunResponse>(cancellationToken: ct);
                var text = result?.Narration ?? result?.Error ?? "Sem resposta";
                var isError = result?.Error != null;
                _chat.AddMessage(isError ? "error" : "assistant", text);
            }
            else
            {
                _chat.AddMessage("error", $"Erro HTTP: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _chat.AddMessage("error", $"Erro de conexão: {ex.Message}");
        }

        _status.Status = "Conectado";
    }

    private async Task SendToBackendAsync(string command, string code, CancellationToken ct)
    {
        try
        {
            var endpoint = command switch
            {
                "/explain" => "/api/coding/explain",
                "/fix" => "/api/coding/fix",
                _ => "/agent/run"
            };

            var response = await _http.PostAsJsonAsync(endpoint,
                new { code, language = "unknown" }, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AgentRunResponse>(cancellationToken: ct);
                var text = result?.Narration ?? result?.Error ?? "Sem resposta";
                _chat.AddMessage("assistant", text);
            }
            else
            {
                _chat.AddMessage("error", $"Erro HTTP: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _chat.AddMessage("error", $"Erro: {ex.Message}");
        }
    }

    private async Task CheckHealthAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            if (response.IsSuccessStatusCode)
            {
                _status.Status = "Conectado";
                _status.RiskLevel = "0.0";
            }
            else
            {
                _status.Status = "Erro";
            }
        }
        catch
        {
            _status.Status = "Desconectado";
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record AgentRunResponse(string? Narration, string? Error);
}
