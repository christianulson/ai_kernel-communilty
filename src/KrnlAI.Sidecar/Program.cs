using KrnlAI.Embedded.Abstractions;
using KrnlAI.Embedded.Services;
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

    await jsonRpc.Completion.ConfigureAwait(false);
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

var configPort = builder.Configuration.GetValue<string>("Sidecar:Port");
var port = PortOptionParser.Resolve(args, configPort).ToString();
app.Urls.Clear();
app.Urls.Add($"http://127.0.0.1:{port}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    // Start gRPC server if --grpc flag
    if (grpcArgs.Contains("--grpc"))
    {
        var grpc = app.Services.GetRequiredService<SidecarGrpcServer>();
        _ = StartGrpcServerAsync(grpc, app.Services.GetRequiredService<ILogger<Program>>());
    }

    var mode = sidecarMode;
    var auth = !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Sidecar:Auth:Token"));
    var proxy = !string.IsNullOrEmpty(builder.Configuration.GetValue<string>("Sidecar:KernelApi:BaseUrl"));
    var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    Console.WriteLine($@"
╔══════════════════════════════════════════╗
║     KrnlAI.Sidecar v{version,-25}║
║     Mode: {mode,-31}║
║     Auth: {(auth ? "enabled" : "disabled"),-29}║
║     KrnlAI API: {(proxy ? "configured" : "unavailable"),-24}║
║     Listening: http://127.0.0.1:{port,-10}║
╚══════════════════════════════════════════╝");
});
app.Lifetime.ApplicationStopping.Register(() => Console.WriteLine("KrnlAI.Sidecar shutting down..."));

async Task StartGrpcServerAsync(SidecarGrpcServer grpc, ILogger<Program> logger)
{
    try
    {
        await grpc.StartAsync(app.Lifetime.ApplicationStopped);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "gRPC server stopped with an error");
    }
}

await app.RunAsync().ConfigureAwait(false);

public partial class Program { }
