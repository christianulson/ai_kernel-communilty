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
root.Add(new SafetyCommand(cliCtx, renderer).Build());
root.Add(new AnticipateCommand(cliCtx, renderer).Build());
root.Add(new IntentionsCommand(cliCtx, renderer).Build());
root.Add(new DebugCommand(cliCtx, renderer).Build());
root.Add(new ServeCommand().Build());

// DX commands (Plano 3)
var templateEngine = host.Services.GetRequiredService<ITemplateEngine>();
var console = host.Services.GetRequiredService<IAnsiConsole>();
root.Add(new NewCommand(templateEngine, console).Build());
root.Add(new InitCommand(templateEngine, console).Build());

// Config command (Plano 14 - YAML Config)
root.Add(new ConfigCommand(console).Build());

// Safety evaluation (Plano 4)
var benchRunner = host.Services.GetRequiredService<SafetyBenchRunner>();
root.Add(new SecurityCommand(benchRunner, console).Build());

return await root.Parse(args).InvokeAsync();
