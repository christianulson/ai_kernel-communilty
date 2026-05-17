using System.CommandLine;
using AIKernel.Cli.Abstractions;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<ConsoleRenderer>();
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
        services.AddCliServices();
        services.AddSingleton<CliSeeder>();
        services.AddSingleton<ITemplateEngine, TemplateEngine>();
    })
    .Build();

// Seed demo data
var seeder = host.Services.GetRequiredService<CliSeeder>();
await seeder.SeedAsync();

var cliCtx = host.Services.GetRequiredService<CliContext>();
var renderer = host.Services.GetRequiredService<ConsoleRenderer>();

var root = new RootCommand("AI Kernel CLI - Developer Interface");

root.Add(new StatusCommand(cliCtx, renderer).Build());
root.Add(new HealthCommand(cliCtx, renderer).Build());
root.Add(new MomentsCommand(cliCtx, renderer).Build());
root.Add(new MemoryCommand(cliCtx, renderer).Build());
root.Add(new SnapshotCommand(cliCtx, renderer).Build());
root.Add(new ArchiveCommand(cliCtx, renderer).Build());
root.Add(new GoalsCommand(cliCtx, renderer).Build());
root.Add(new SafetyCommand(cliCtx, renderer, host.Services).Build());
root.Add(new AnticipateCommand(cliCtx, renderer).Build());
root.Add(new IntentionsCommand(cliCtx, renderer).Build());
root.Add(new DebugCommand(cliCtx, renderer).Build());
root.Add(new ServeCommand().Build());

// DX commands (Plano 3)
var templateEngine = host.Services.GetRequiredService<ITemplateEngine>();
var console = host.Services.GetRequiredService<IAnsiConsole>();
root.Add(new NewCommand(templateEngine, console).Build());
root.Add(new InitCommand(templateEngine, console).Build());
root.Add(new TemplatesCommand(templateEngine).Build());

// Config command (Plano 14 - YAML Config)
root.Add(new ConfigCommand(console).Build());

// Safety evaluation (Plano 4)
var benchRunner = host.Services.GetRequiredService<SafetyBenchRunner>();
root.Add(new SecurityCommand(benchRunner, console).Build());

// Integration management (Plano 02 - Ecosystem)
root.Add(new IntegrationCommand(console).Build());

// Plugin management (Plano 37 - Plugin Ecosystem)
var pluginLoader = host.Services.GetService<Kernel.Core.Abstractions.IAssemblyPluginLoader>();
root.Add(new PluginCommand(console, pluginLoader).Build());

// MCP management (Track B2)
root.Add(new McpCommand(cliCtx, renderer).Build());

// Model management (Track B3)
root.Add(new ModelCommand(cliCtx, renderer).Build());

// TUI Interactive mode (Plano 6)
root.Add(new InteractiveCommand().Build());

// Upgrade (Plano 12)
root.Add(new UpgradeCommand().Build());

// Provider management (Plano 12)
root.Add(new ProviderCommand().Build());

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
root.Add(new SessionCommand(console, sessionStore).Build());

return await root.Parse(args).InvokeAsync();
