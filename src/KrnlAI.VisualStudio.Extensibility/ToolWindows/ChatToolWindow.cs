using KrnlAI.VisualStudio.Extensibility.Core.Chat;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Krnl-AI chat tool window (Remote UI), connected to the kernel.</summary>
[VisualStudioContribution]
public sealed class ChatToolWindow : ToolWindow
{
    private readonly KernelClientService _kernel;
    private readonly KernelMessageSink _sink;
    private readonly ChatToolWindowContent _content;

    /// <summary>Creates a new instance.</summary>
    public ChatToolWindow(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
        Title = "Krnl-AI Chat";
        _kernel = new KernelClientService(new HttpClient(), maxRetries: 2);
        _sink = new KernelMessageSink(_kernel);
        var session = new ChatSession(_sink);
        _content = new ChatToolWindowContent(new ChatDataContext(session, _sink));
    }

    /// <inheritdoc/>
    public override ToolWindowConfiguration ToolWindowConfiguration => new()
    {
        Placement = ToolWindowPlacement.DocumentWell,
        AllowAutoCreation = true
    };

    /// <inheritdoc/>
    public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        => Task.FromResult<IRemoteUserControl>(_content);

    /// <inheritdoc/>
    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var endpoint = KernelEndpointResolver.Resolve(KernelRuntimeMode.LocalApi, null, 5001);
        await _kernel.ConnectAsync(endpoint, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _content.Dispose();
            _kernel.Dispose();
        }

        base.Dispose(disposing);
    }
}