using System.CommandLine;
using AIKernel.Cli.Abstractions;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;

namespace AIKernel.Cli.Tests;

public sealed class TemplatesCommandTests
{
    [Fact]
    public void TemplatesCommand_Build_ShouldReturnCommand()
    {
        var engine = new TemplateEngine(NullLogger<TemplateEngine>.Instance);
        var cmd = new TemplatesCommand(engine).Build();
        Assert.NotNull(cmd);
        Assert.Equal("templates", cmd.Name);
    }

    [Fact]
    public async Task TemplatesCommand_Engine_ShouldListTemplates()
    {
        var engine = new TemplateEngine(NullLogger<TemplateEngine>.Instance);
        var templates = await engine.ListTemplatesAsync();
        Assert.NotEmpty(templates);
        Assert.Contains(templates, t => t.Type == TemplateType.Agent);
    }
}
