using AIKernel.Sidecar;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(o => o.Limits.MaxRequestBodySize = 1024 * 1024);

builder.Services.AddSidecarServices(builder.Configuration, builder.Environment);

var app = builder.Build();
app.ConfigureSidecarPipeline();
app.MapSidecarEndpoints();

var port = "5001";
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) { port = args[i + 1]; break; }
    if (args[i].StartsWith("--port=", StringComparison.OrdinalIgnoreCase)) { port = args[i]["--port=".Length..]; break; }
}
app.Urls.Clear();
app.Urls.Add($"http://127.0.0.1:{port}");

app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine($"AIKernel.Sidecar started on http://127.0.0.1:{port}"));
app.Lifetime.ApplicationStopping.Register(() => Console.WriteLine("AIKernel.Sidecar shutting down..."));

await app.RunAsync();

public partial class Program { }
