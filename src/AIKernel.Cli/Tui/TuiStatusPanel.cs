using Spectre.Console;

namespace AIKernel.Cli.Tui;

public sealed class TuiStatusPanel
{
    public string Status { get; set; } = "Desconectado";
    public string RiskLevel { get; set; } = "0.0";
    public string Mode { get; set; } = "Chat";
    public string Mood { get; set; } = "Neutro";
    public string MemoryCount { get; set; } = "0";
    public string LastAction { get; set; } = "nenhuma";
    public int MessageCount { get; set; }

    public Panel Render()
    {
        var danger = double.TryParse(RiskLevel, out var r) ? r : 0;
        var riskColor = danger switch
        {
            < 0.3 => "green",
            < 0.7 => "yellow",
            _ => "red"
        };

        var statusIcon = Status switch
        {
            "Conectado" => "[green]●[/]",
            "Processando..." => "[yellow]◐[/]",
            "Erro" => "[red]●[/]",
            _ => "[grey]○[/]"
        };

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap().PadRight(2))
            .AddColumn();

        grid.AddRow("Status:", $"{statusIcon} {Status.EscapeMarkup()}");
        grid.AddRow("Risco:", $"[{riskColor}]{RiskLevel.EscapeMarkup()}[/]");
        grid.AddRow("Modo:", Mode.EscapeMarkup());
        grid.AddRow("Humor:", Mood.EscapeMarkup());
        grid.AddRow("Memória:", MemoryCount.EscapeMarkup());
        grid.AddRow("Mensagens:", MessageCount.ToString());
        grid.AddRow("Última ação:", LastAction.EscapeMarkup());

        return new Panel(grid)
            .Header(" Status ")
            .Border(BoxBorder.Rounded)
            .Expand();
    }
}
