using KrnlAI.Embedded.Abstractions;
using System.Text.Json;

namespace KrnlAI.Sidecar.Rpc;

public sealed class SidecarGrpcServer(IEmbeddedKrnlAI kernel, ILogger<SidecarGrpcServer> logger, int port = 5004)
{
    private readonly int _port = port;
    private CancellationTokenSource? _cts;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        logger.LogInformation("Sidecar gRPC server starting on port {Port}...", _port);

        // Simple JSON-RPC over HTTP for now (full gRPC requires Grpc.AspNetCore package)
        var listener = new System.Net.HttpListener();
        listener.Prefixes.Add($"http://localhost:{_port}/");
        listener.Start();
        logger.LogInformation("Sidecar gRPC listening on http://localhost:{Port}", _port);

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
                var context = await listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
                _ = HandleRequestAsync(context, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sidecar gRPC listener error");
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
                await using var sw = new StreamWriter(response.OutputStream);
                await sw.WriteAsync(JsonSerializer.Serialize(new { status = "SERVING", service = "sidecar-grpc" })).ConfigureAwait(false);
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/v1/agent/run")
            {
                using var reader = new System.IO.StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                var agentRequest = JsonSerializer.Deserialize<GrpcAgentRunRequest>(body);

                if (agentRequest == null)
                {
                    response.StatusCode = 400;
                    await WriteJson(response, new { error = "Invalid request" }).ConfigureAwait(false);
                    return;
                }

                var result = await kernel.RunAsync(agentRequest.Input, ct).ConfigureAwait(false);
                response.StatusCode = 200;
                await WriteJson(response, new
                {
                    agentId = agentRequest.AgentId,
                    output = result.Narration,
                    steps = result.Steps,
                    success = result.Error == null,
                    error = result.Error,
                    mode = result.Mode
                }).ConfigureAwait(false);
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/v1/memory/search")
            {
                using var reader = new System.IO.StreamReader(request.InputStream);
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                var searchRequest = JsonSerializer.Deserialize<GrpcMemorySearchRequest>(body);

                if (searchRequest == null)
                {
                    response.StatusCode = 400;
                    await WriteJson(response, new { error = "Invalid request" }).ConfigureAwait(false);
                    return;
                }

                var hits = await kernel.SearchMemoryAsync(searchRequest.Query, ct).ConfigureAwait(false);
                response.StatusCode = 200;
                await WriteJson(response, new { hits = hits.Select(h => new { id = h.Id, score = h.Score, content = h.Payload }) }).ConfigureAwait(false);
                return;
            }

            response.StatusCode = 404;
            await WriteJson(response, new { error = "Not found" }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sidecar gRPC request failed");
            try
            {
                context.Response.StatusCode = 500;
                await WriteJson(context.Response, new { error = ex.Message }).ConfigureAwait(false);
            }
            catch { }
        }
    }

    private static async Task WriteJson(System.Net.HttpListenerResponse response, object data)
    {
        response.ContentType = "application/json";
        await using var sw = new StreamWriter(response.OutputStream);
        await sw.WriteAsync(JsonSerializer.Serialize(data,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })).ConfigureAwait(false);
    }

    private sealed record GrpcAgentRunRequest(string AgentId, string Input);
    private sealed record GrpcMemorySearchRequest(string Query, int TopK = 10, double Threshold = 0.0);
}
