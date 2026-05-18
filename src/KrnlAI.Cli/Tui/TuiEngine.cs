using System.Net.Http.Json;
using KrnlAI.Embedded;
using Spectre.Console;

namespace KrnlAI.Cli.Tui;

public sealed class TuiEngine
{
    private readonly TuiChatPanel _chat = new();
    private readonly TuiStatusPanel _status = new();
    private readonly TuiSplitView _splitView = new();
    private readonly TuiSessionStore _sessionStore = new();
    private readonly TuiInputHandler _inputHandler;
    private readonly HttpClient? _http;
    private readonly EmbeddedKernel? _kernel;
    private readonly string _baseUrl;
    private readonly bool _isLocal;
    private bool _running;

    public TuiEngine(string baseUrl = "http://localhost:5000")
    {
        _baseUrl = baseUrl;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };

        var commands = new Dictionary<string, string>
        {
            ["/help"] = "Mostra esta ajuda",
            ["/explain"] = "Explica código (cole o código após o comando)",
            ["/fix"] = "Tenta corrigir código com problemas",
            ["/status"] = "Mostra status detalhado do kernel",
            ["/clear"] = "Limpa o chat",
            ["/sessions"] = "Lista sessões salvas",
            ["/session"] = "Carrega sessão: /session <id>",
            ["/connect"] = "Conecta ao backend (URL opcional)",
            ["/exit"] = "Sai do modo interativo",
            ["/quit"] = "Sai do modo interativo",
        };
        _inputHandler = new TuiInputHandler(commands);
    }

    public TuiEngine(EmbeddedKernel kernel)
    {
        _baseUrl = "embedded://local";
        _kernel = kernel;
        _isLocal = true;
        var commands = new Dictionary<string, string>
        {
            ["/help"] = "Mostra esta ajuda",
            ["/status"] = "Mostra status detalhado do kernel",
            ["/clear"] = "Limpa o chat",
            ["/exit"] = "Sai do modo interativo",
            ["/quit"] = "Sai do modo interativo",
        };
        _inputHandler = new TuiInputHandler(commands);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _running = true;
        _chat.AddMessage("system", "Krnl-AI TUI iniciado. Digite /help para comandos.");

        await CheckHealthAsync(ct);
        await LoadLastSessionAsync();

        while (_running && !ct.IsCancellationRequested)
        {
            RenderLayout();

            var input = _inputHandler.ReadInputWithAutocomplete();

            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(input)) continue;

            var (cmd, args) = _inputHandler.Parse(input);

            if (!string.IsNullOrEmpty(cmd))
                await HandleSlashCommandAsync(cmd, args, ct);
            else
                await SendMessageAsync(input, ct);

            await AutoSaveSessionAsync();
            await CheckHealthAsync(ct);
        }

        _chat.AddMessage("system", "TUI encerrado.");
        RenderLayout();
    }

    private void RenderLayout()
    {
        AnsiConsole.Clear();
        _splitView.Clear();
        _splitView.AddPanel(() => _chat.Render());
        _splitView.AddPanel(() => _status.Render());
        _splitView.Render();
        AnsiConsole.MarkupLine(new string('-', Console.WindowWidth));
    }

    private async Task HandleSlashCommandAsync(string command, string args, CancellationToken ct)
    {
        switch (command)
        {
            case "/help":
                _chat.AddMessage("system", "Comandos disponíveis:");
                foreach (var (cmd, desc) in _inputHandler.GetCommandDescriptions())
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

            case "/sessions":
                await ListSessionsAsync();
                break;

            case "/session":
                if (string.IsNullOrWhiteSpace(args))
                {
                    _chat.AddMessage("error", "Uso: /session <id>. Execute /sessions para listar.");
                }
                else
                {
                    await LoadSessionAsync(args);
                }
                break;

            case "/connect":
                if (_isLocal)
                {
                    _chat.AddMessage("system", "Modo local ativo; /connect não altera o EmbeddedKernel.");
                    await CheckHealthAsync(ct);
                    break;
                }
                if (!string.IsNullOrWhiteSpace(args))
                {
                    _http!.BaseAddress = new Uri(args);
                    _chat.AddMessage("system", $"Tentando conectar em: {args}");
                }
                await CheckHealthAsync(ct);
                break;

            case "/exit":
            case "/quit":
                await AutoSaveSessionAsync();
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
            if (_isLocal)
            {
                var result = await _kernel!.RunAsync(message, ct);
                _chat.AddMessage(result.Error is null ? "assistant" : "error", result.Error ?? result.Narration);
                _status.Status = "Local";
                return;
            }

            var response = await _http!.PostAsJsonAsync("/agent/run",
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

            if (_isLocal)
            {
                var result = await _kernel!.RunAsync($"{command}\n{code}", ct);
                _chat.AddMessage(result.Error is null ? "assistant" : "error", result.Error ?? result.Narration);
                return;
            }

            var response = await _http!.PostAsJsonAsync(endpoint,
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
            if (_isLocal)
            {
                _status.Status = "Local";
                _status.RiskLevel = "0.0";
                _status.Mode = "community";
                return;
            }

            var response = await _http!.GetAsync("/health", ct);
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

    private async Task AutoSaveSessionAsync()
    {
        if (_chat.MessageCount == 0) return;
        var messages = _chat.Messages.ToList();
        if (messages.Count == 0) return;
        var label = $"Chat {DateTimeOffset.Now:yyyy-MM-dd HH:mm}";
        await _sessionStore.SaveAsync(label, messages);
    }

    private async Task LoadLastSessionAsync()
    {
        var sessions = await _sessionStore.ListAsync();
        if (sessions.Count == 0) return;
        var last = sessions[0];
        foreach (var msg in last.Messages)
            _chat.AddMessage(msg.Role, msg.Content, msg.IsError);
        _chat.AddMessage("system", $"Sessão anterior carregada: {last.Label} ({last.MessageCount} mensagens)");
    }

    private async Task ListSessionsAsync()
    {
        var sessions = await _sessionStore.ListAsync();
        if (sessions.Count == 0)
        {
            _chat.AddMessage("system", "Nenhuma sessão salva.");
            return;
        }
        _chat.AddMessage("system", $"Sessões salvas ({sessions.Count}):");
        foreach (var s in sessions.Take(10))
            _chat.AddMessage("system", $"  {s.Id,-12} {s.Label} ({s.MessageCount} msgs)");
    }

    private async Task LoadSessionAsync(string sessionId)
    {
        var session = await _sessionStore.LoadAsync(sessionId);
        if (session == null)
        {
            _chat.AddMessage("error", $"Sessão não encontrada: {sessionId}");
            return;
        }
        _chat.Clear();
        foreach (var msg in session.Messages)
            _chat.AddMessage(msg.Role, msg.Content, msg.IsError);
        _chat.AddMessage("system", $"Sessão carregada: {session.Label} ({session.MessageCount} mensagens)");
    }

    public void Dispose()
    {
        _http?.Dispose();
        _kernel?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private sealed record AgentRunResponse(string? Narration, string? Error);
}
