using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;
using System.Text.Json;

namespace KrnlAI.Sidecar.Rpc;

public sealed class SidecarGrpcServer
{
    private readonly IEmbeddedKrnlAI _kernel;
    private readonly ILogger<SidecarGrpcServer> _logger;
    private readonly int _port;
    private CancellationTokenSource? _cts;

    public SidecarGrpcServer(IEmbeddedKrnlAI kernel, ILogger<SidecarGrpcServer> logger, int port = 5004)
    {
        _kernel = kernel;
        _logger = logger;
        _port = port;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _logger.LogInformation("Sidecar gRPC server starting on port {Port}...", _port);

        // Simple JSON-RPC over HTTP for now (full gRPC requires Grpc.AspNetCore package)
        var listener = new System.Net.HttpListener();
        listener.Prefixes.Add($"http://localhost:{_port}/");
        listener.Start();
        _logger.LogInformation("Sidecar gRPC listening on http://localhost:{Port}", _port);

        _ = Task.Run(() => ListenLoop(listener, _cts.Token), _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task ListenLoop(System.Net.HttpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequestAsync(context, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sidecar gRPC listener error");
            }
        }
    }

    private async Task HandleRequestAsync(System.Net.HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/health")
            {
                response.StatusCode = 200;
                await using var sw = new System.IO.StreamWriter(response.OutputStream);
                await sw.WriteAsync(JsonSerializer.Serialize(new { status = "SERVING", service = "sidecar-grpc" }));
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/v1/agent/run")
            {
                using var reader = new System.IO.StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync();
                var agentRequest = JsonSerializer.Deserialize<GrpcAgentRunRequest>(body);

                if (agentRequest == null)
                {
                    response.StatusCode = 400;
                    await WriteJson(response, new { error = "Invalid request" });
                    return;
                }

                var result = await _kernel.RunAsync(agentRequest.Input, ct);
                response.StatusCode = 200;
                await WriteJson(response, new
                {
                    agentId = agentRequest.AgentId,
                    output = result.Narration,
                    steps = result.Steps,
                    success = result.Error == null,
                    error = result.Error,
                    mode = result.Mode
                });
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/v1/memory/search")
            {
                using var reader = new System.IO.StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync();
                var searchRequest = JsonSerializer.Deserialize<GrpcMemorySearchRequest>(body);

                if (searchRequest == null)
                {
                    response.StatusCode = 400;
                    await WriteJson(response, new { error = "Invalid request" });
                    return;
                }

                var hits = await _kernel.SearchMemoryAsync(searchRequest.Query, ct);
                response.StatusCode = 200;
                await WriteJson(response, new { hits = hits.Select(h => new { id = h.Id, score = h.Score, content = h.Payload }) });
                return;
            }

            response.StatusCode = 404;
            await WriteJson(response, new { error = "Not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sidecar gRPC request failed");
            try
            {
                context.Response.StatusCode = 500;
                await WriteJson(context.Response, new { error = ex.Message });
            }
            catch { }
        }
    }

    private static async Task WriteJson(System.Net.HttpListenerResponse response, object data)
    {
        response.ContentType = "application/json";
        await using var sw = new System.IO.StreamWriter(response.OutputStream);
        await sw.WriteAsync(JsonSerializer.Serialize(data,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private sealed record GrpcAgentRunRequest(string AgentId, string Input);
    private sealed record GrpcMemorySearchRequest(string Query, int TopK = 10, double Threshold = 0.0);
}
