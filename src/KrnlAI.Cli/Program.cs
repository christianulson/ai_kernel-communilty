using System.CommandLine;
using KrnlAI.Cli.Abstractions;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

var apiBaseUrl = args.Contains("--endpoint") ? args[Array.IndexOf(args, "--endpoint") + 1] : "http://localhost:5235";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<ConsoleRenderer>();
        services.AddSingleton(AnsiConsole.Console);
        services.AddCliServices();
        services.AddSingleton<CliSeeder>();
        services.AddSingleton<ITemplateEngine, TemplateEngine>();
        services.AddHttpClient("KernelApi", c => c.BaseAddress = new Uri(apiBaseUrl));
        services.AddSingleton(s => s.GetRequiredService<IHttpClientFactory>().CreateClient("KernelApi"));
    })
    .Build();

// Seed demo data
var seeder = host.Services.GetRequiredService<CliSeeder>();
await seeder.SeedAsync().ConfigureAwait(false);

var cliCtx = host.Services.GetRequiredService<CliContext>();
var renderer = host.Services.GetRequiredService<ConsoleRenderer>();

var root = new RootCommand("Krnl-AI CLI - Developer Interface")
{
    new StatusCommand(cliCtx, renderer).Build(),
    new HealthCommand(cliCtx, renderer).Build(),
    new MomentsCommand(cliCtx, renderer).Build(),
    new MemoryCommand(cliCtx, renderer).Build(),
    new SnapshotCommand(cliCtx, renderer).Build(),
    new ArchiveCommand(cliCtx, renderer).Build(),
    new GoalsCommand(cliCtx, renderer).Build(),
    new SafetyCommand(cliCtx, renderer, host.Services).Build(),
    new ScheduleCommand(cliCtx, host.Services.GetRequiredService<IAnsiConsole>()).Build(),
    new AnticipateCommand(cliCtx, renderer).Build(),
    new IntentionsCommand(cliCtx, renderer).Build(),
    new DebugCommand(cliCtx, renderer).Build(),
    new DocumentCommand(cliCtx, renderer).Build(),
    new ServeCommand().Build()
};

// DX commands (Plano 3)
var templateEngine = host.Services.GetRequiredService<ITemplateEngine>();
var console = host.Services.GetRequiredService<IAnsiConsole>();
root.Add(new NewCommand(templateEngine, console).Build());
root.Add(new InitCommand(templateEngine, console).Build());
root.Add(new TemplatesCommand(templateEngine).Build());

// Config command (Plano 14 - YAML Config)
var managedSettingsChain = host.Services.GetService<KrnlAI.Core.Config.ManagedSettingsChain>();
root.Add(new ConfigCommand(console, managedSettingsChain).Build());

// Safety evaluation (Plano 4)
var benchRunner = host.Services.GetRequiredService<SafetyBenchRunner>();
root.Add(new SecurityCommand(benchRunner, console).Build());

// Benchmark (Plano 08 - Safety Benchmark)
var reportGen = host.Services.GetRequiredService<KrnlAI.Core.Abstractions.Safety.ISafetyReportGenerator>();
root.Add(new BenchmarkCommand(benchRunner, reportGen, console).Build());

// Integration management (Plano 02 - Ecosystem)
root.Add(new IntegrationCommand(console).Build());

// Plugin management (Plano 37 - Plugin Ecosystem)
var pluginLoader = host.Services.GetService<KrnlAI.Core.Abstractions.IAssemblyPluginLoader>();
var pluginCatalog = host.Services.GetService<KrnlAI.Core.Abstractions.IPluginCatalog>();
var pluginRegistry = host.Services.GetService<KrnlAI.Core.Abstractions.IPluginRegistryService>();
root.Add(new PluginCommand(console, pluginLoader, pluginCatalog, pluginRegistry).Build());

// MCP management (Track B2)
var mcpServerHost = host.Services.GetService<KrnlAI.Core.Abstractions.Mcp.IMcpServerHost>();
root.Add(new McpCommand(cliCtx, renderer, mcpServerHost).Build());

// Plan/Act mode
root.Add(new PlanCommand(cliCtx, renderer).Build());

// Model management (Track B3)
root.Add(new ModelCommand(cliCtx, renderer).Build());

// TUI Interactive mode (Plano 6)
root.Add(new InteractiveCommand().Build());

// Upgrade (Plano 12)
root.Add(new UpgradeCommand().Build());

// Provider management (Plano 12)
root.Add(new ProviderCommand().Build());

// Profile (Gap fix)
root.Add(new ProfileCommand().Build());

// Export (Plano 11)
root.Add(new ExportCommand().Build());

// Stats (Plano 11)
root.Add(new CliStatsCommand().Build());

// Non-interactive / Pipe mode (Claude Code)
root.Add(new RunCommand().Build());

// Eval (Plano 05 - Non-Interactive Mode + Piping)
root.Add(new EvalCommand().Build());

// PR Review (Plano 05 - GitHub Code Review)
root.Add(new ReviewPrCommand().Build());

// Local diff review (Plano 05 - GitHub Code Review)
root.Add(new ReviewCommand().Build());

// Experiment management (Track B4)
root.Add(new ExperimentCommand(cliCtx, renderer).Build());

// Session management (Track B5)
var sessionStore = host.Services.GetRequiredService<InMemorySessionStore>();
var cognitiveSessionStore = host.Services.GetRequiredService<KrnlAI.Cognition.Contracts.ISessionStore>();
root.Add(new SessionCommand(console, sessionStore, cognitiveSessionStore).Build());

// Lifecycle hooks management
var lifecycleOrchestrator = host.Services.GetRequiredService<KrnlAI.Core.Services.Lifecycle.LifecycleOrchestrator>();
root.Add(new LifecycleCommand(lifecycleOrchestrator, renderer).Build());

// Checkpoint management
root.Add(new CheckpointCommand(cliCtx, renderer).Build());

return await root.Parse(args).InvokeAsync().ConfigureAwait(false);
