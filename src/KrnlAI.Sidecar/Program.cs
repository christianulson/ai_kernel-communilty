using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;
using KrnlAI.Sidecar;
using KrnlAI.Sidecar.Rpc;
using StreamJsonRpc;

var stdioMode = args.Any(a => a == "--stdio");

if (stdioMode)
{
#pragma warning disable ASP0000
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
    services.AddSingleton<IEmbeddedKrnlAI>(new EmbeddedKrnlAI());
    services.AddSingleton<SidecarRpcHandler>();
    var sp = services.BuildServiceProvider();

    var handler = sp.GetRequiredService<SidecarRpcHandler>();
    var jsonRpc = JsonRpc.Attach(Console.OpenStandardInput(), Console.OpenStandardOutput(), handler);
    jsonRpc.StartListening();

    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("KrnlAI.Sidecar started in stdio/RPC mode");

    await jsonRpc.Completion;
    logger.LogInformation("KrnlAI.Sidecar stdio mode shutting down...");
    return;
#pragma warning restore ASP0000
}

// HTTP mode
var builder = WebApplication.CreateBuilder(args);

var sidecarMode = builder.Configuration.GetValue<string>("Sidecar:Mode", "Legacy");
var communityMode = string.Equals(sidecarMode, "Community", StringComparison.OrdinalIgnoreCase);

if (communityMode)
    builder.Services.AddSidecarCommunityServices(builder.Configuration, builder.Environment);
else
    builder.Services.AddSidecarServices(builder.Configuration, builder.Environment);

// gRPC server (JSON-RPC over HTTP for embedded mode)
var grpcArgs = args.ToList();
if (grpcArgs.Contains("--grpc"))
{
    builder.Services.AddSingleton<SidecarGrpcServer>();
    builder.Services.AddSingleton<IEmbeddedKrnlAI>(_ => new EmbeddedKrnlAI());
}

// Swagger (Development only)
    if (builder.Environment.IsDevelopment() || communityMode)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

var app = builder.Build();

// Swagger UI
    if (builder.Environment.IsDevelopment() || communityMode)
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
    // Start gRPC server if --grpc flag
    if (grpcArgs.Contains("--grpc"))
    {
        var grpc = app.Services.GetRequiredService<SidecarGrpcServer>();
        _ = Task.Run(() => grpc.StartAsync(app.Lifetime.ApplicationStopped));
    }

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
