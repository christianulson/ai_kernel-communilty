using System.CommandLine;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
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
    })
    .Build();

// Seed demo data
var seeder = host.Services.GetRequiredService<CliSeeder>();
await seeder.SeedAsync();

var cliCtx = host.Services.GetRequiredService<CliContext>();
var renderer = host.Services.GetRequiredService<ConsoleRenderer>();

var root = new RootCommand("AI Kernel CLI - Operational Interface");

root.Add(new StatusCommand(cliCtx, renderer).Build());
root.Add(new HealthCommand(cliCtx, renderer).Build());
root.Add(new MomentsCommand(cliCtx, renderer).Build());
root.Add(new MemoryCommand(renderer).Build());
root.Add(new SnapshotCommand(renderer).Build());
root.Add(new ArchiveCommand(renderer).Build());
root.Add(new GoalsCommand(renderer).Build());
root.Add(new SafetyCommand(renderer).Build());
root.Add(new AnticipateCommand(renderer).Build());
root.Add(new IntentionsCommand(renderer).Build());
root.Add(new DebugCommand(renderer).Build());
root.Add(new ServeCommand().Build());

return await root.Parse(args).InvokeAsync();
