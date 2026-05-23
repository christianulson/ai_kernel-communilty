#pragma warning disable ASP0000
using KrnlAI.Embedded;
using KrnlAI.Sidecar;
using KrnlAI.Sidecar.Rpc;
using Microsoft.Extensions.DependencyInjection;
using StreamJsonRpc;

var stdioMode = args.Any(a => a == "--stdio");

if (stdioMode)
{
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
    services.AddSingleton(new EmbeddedKrnlAI());
    services.AddSingleton<SidecarRpcHandler>();
    var sp = services.BuildServiceProvider();

    var handler = sp.GetRequiredService<SidecarRpcHandler>();
    var jsonRpc = JsonRpc.Attach(Console.OpenStandardInput(), Console.OpenStandardOutput(), handler);
    jsonRpc.StartListening();

    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("KrnlAI.Sidecar started in stdio/RPC mode");

    // Keep process alive until the RPC connection closes
    await jsonRpc.Completion;
    logger.LogInformation("KrnlAI.Sidecar stdio mode shutting down...");
    return;
}

// HTTP mode (existing logic)
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(o => o.Limits.MaxRequestBodySize = 1024 * 1024);

var sidecarMode = builder.Configuration.GetValue<string>("Sidecar:Mode", "Legacy");
var communityMode = string.Equals(sidecarMode, "Community", StringComparison.OrdinalIgnoreCase);

if (communityMode)
    builder.Services.AddSidecarCommunityServices(builder.Configuration);
else
    builder.Services.AddSidecarServices(builder.Configuration, builder.Environment);

// Swagger (Development only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Swagger UI
if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KrnlAI.Sidecar v1"));
}
if (communityMode)
    app.MapCommunityEndpoints();
else
{
    app.ConfigureSidecarPipeline();
    app.MapSidecarEndpoints();
}

var port = "5001";
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) { port = args[i + 1]; break; }
    if (args[i].StartsWith("--port=", StringComparison.OrdinalIgnoreCase)) { port = args[i]["--port=".Length..]; break; }
}
app.Urls.Clear();
app.Urls.Add($"http://127.0.0.1:{port}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    var mode = sidecarMode;
    var auth = !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Sidecar:Auth:Token"));
    var proxy = !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Sidecar:KernelApi:BaseUrl"));
    Console.WriteLine($@"
╔══════════════════════════════════════════╗
║     KrnlAI.Sidecar v1.0.0               ║
║     Mode: {mode,-31}║
║     Auth: {(auth ? "enabled" : "disabled"),-29}║
║     KrnlAI API: {(proxy ? "configured" : "unavailable"),-24}║
║     Listening: http://127.0.0.1:{port,-10}║
╚══════════════════════════════════════════╝");
});
app.Lifetime.ApplicationStopping.Register(() => Console.WriteLine("KrnlAI.Sidecar shutting down..."));

await app.RunAsync();

public partial class Program { }
