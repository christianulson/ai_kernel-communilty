using System.Threading.RateLimiting;
using AIKernel.Sidecar;
using Kernel.Abstractions;
using Kernel.Core.Services.Safety;
using Kernel.Infrastructure.InMemory;
using Kernel.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IAdversarialGuard, AdversarialGuard>();
builder.Services.AddSingleton<IRiskScorer, SimpleRiskScorer>();
builder.Services.AddSingleton<IPolicyStore, InMemoryPolicyStore>();
builder.Services.AddSingleton<IStateStore, InMemoryStateStore>();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new() { PermitLimit = 60, Window = TimeSpan.FromSeconds(10) }));
    options.RejectionStatusCode = 429;
});

var app = builder.Build();
app.UseRateLimiter();
app.Use(async (ctx, next) => { ctx.Response.Headers["X-Content-Type-Options"] = "nosniff"; ctx.Response.Headers["X-Frame-Options"] = "DENY"; await next(); });
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path != "/health")
    {
        var auth = ctx.Request.Headers["Authorization"].FirstOrDefault();
        var token = Environment.GetEnvironmentVariable("AIKERNEL_AUTH_TOKEN");
        if (!string.IsNullOrEmpty(token) && auth != $"Bearer {token}") { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsJsonAsync(new { error = "unauthorized" }); return; }
    }
    await next();
});

// 🩺 Health
app.MapGet("/health", () => Results.Ok(new { status = "ok", ts = DateTime.UtcNow, version = "AIKernel.Sidecar/1.0.0" }));

// 🤖 Agent run
app.MapPost("/agent/run", async (HttpContext ctx, IAdversarialGuard guard) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<AgentRunRequest>();
    if (body == null) return Results.BadRequest(new { error = "invalid_request" });
    var safetyResult = await guard.ValidateAsync(body.Prompt ?? "", ctx.RequestAborted);
    var blocked = !safetyResult.IsAllowed;
    return Results.Ok(new AgentRunResponse
    {
        Narration = blocked ? "Conteúdo bloqueado." : BuildNarration(body.Prompt ?? ""),
        Error = blocked ? "safety_block" : null,
        TransportSteps = new[] { new TransportStepDto { Label = "Segurança", Detail = blocked ? "BLOQUEADO" : "OK", Ok = !blocked } },
        ActiveStages = new[] { "standalone" }
    });
});

// 📋 Policies
app.MapGet("/policy/list", (string? domain) =>
    Results.Ok(new { policies = new[] { new { id = "p1", name = "Default Policy", domain = "general", version = "1.0", createdAt = DateTime.UtcNow, isActive = true } }, totalCount = 1 }));

// 📊 Scorecard
app.MapGet("/agent/metrics/scorecard", () => Results.Ok(new { reliability = 0.85, efficiency = 0.78, safety = 0.95, antiLoop = 0.88, governance = 0.82, overall = 0.86 }));

// 🧠 Memory
app.MapPost("/memory/search", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, object>>();
    return Results.Ok(new { hits = Array.Empty<object>(), totalCount = 0 });
});
app.MapGet("/memory/metrics", () => Results.Ok(new { totalChunks = 0, totalDocuments = 0, totalSizeBytes = 0 }));

// 📜 Episodes
app.MapGet("/episodes/search", () => Results.Ok(new { episodes = Array.Empty<object>(), totalCount = 0 }));
app.MapGet("/episodes/{id}", (string id) => Results.Ok(new { id, goalId = "standalone", status = "idle", createdAt = DateTime.UtcNow }));

var port = args switch { ["--port", var p] => p, _ => "5001" };
try { app.Run($"https://localhost:{port}"); }
catch { Console.WriteLine($"HTTPS unavailable, falling back to HTTP on {port}"); app.Run($"http://127.0.0.1:{port}"); }

static string BuildNarration(string prompt) => prompt.ToLower() switch
{
    var p when p.Contains("olá") || p.Contains("oi") => "Olá! Modo standalone local ativo. Como posso ajudar?",
    var p when p.Contains("ajuda") => "Modo standalone: segurança e risco ativos. Backend completo requer conexão remota.",
    var p when p.Contains("quem é") => "AI Kernel em modo standalone. Safety ativo.",
    _ => "Processado em modo standalone."
};
