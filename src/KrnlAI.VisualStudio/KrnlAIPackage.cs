#if AUTOCODE_ENABLE_CODELENS
using System.Runtime.InteropServices;
using KrnlAI.VisualStudio.Commands;
using KrnlAI.VisualStudio.Options;
using KrnlAI.VisualStudio.Services;
using KrnlAI.VisualStudio.ToolWindows;
using KrnlAI.VisualStudio.ToolWindows.Dashboard;
using KrnlAI.VisualStudio.ToolWindows.Policies;
using KrnlAI.VisualStudio.ToolWindows.Episodes;
using KrnlAI.VisualStudio.ToolWindows.Kanban;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio;

[Guid(PackageGuid)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideToolWindow(typeof(KrnlAIToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300)]
[ProvideToolWindow(typeof(DashboardToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 400)]
[ProvideToolWindow(typeof(PoliciesToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 400)]
[ProvideToolWindow(typeof(EpisodesToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 400)]
[ProvideToolWindow(typeof(KanbanToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 500)]
[ProvideOptionPage(typeof(KrnlAIOptionsPage), "Krnl-AI", "General", 0, 0, true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideService(typeof(IEditorContextProvider), ServiceName = "KrnlAI Editor Context Provider")]
[ProvideService(typeof(IVsCommandHandler), ServiceName = "KrnlAI Command Handler")]
public sealed class KrnlAIPackage : AsyncPackage
{
    public const string PackageGuid = "a6b3f8e1-2c4d-4e5f-8a9b-0c1d2e3f4a5b";

    private EditorContextProvider? _editorContextProvider;
    private VsCommandHandler? _commandHandler;
    private ISettingsService? _settings;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _settings = new SettingsService();
        _settings.Load();

        _editorContextProvider = new EditorContextProvider();
        _commandHandler = CreateCommandHandler();

        await ShowToolWindowAsync(typeof(KrnlAIToolWindow), 0, true, cancellationToken);
        await SendSelectionToChat.InitializeAsync(this);
        await AnalyzeErrorCommand.InitializeAsync(this);
        await OpenKanbanCommand.InitializeAsync(this);
    }

    private VsCommandHandler CreateCommandHandler()
    {
        var httpClient = new HttpClient();
        var client = new KernelClientService(httpClient);
        var context = new SolutionContextService(this);
        var applyEdit = new ApplyEditService();
        var agenticLoop = new AgenticLoopService(client);
        var terminal = new TerminalService();
        var git = new GitService();

        if (_settings is not null)
        {
            var endpoint = KernelEndpointResolver.Resolve(
                _settings.RuntimeMode,
                _settings.Endpoint,
                _settings.SidecarPort);
            _ = client.ConnectAsync(endpoint);
        }

        return new VsCommandHandler(client, context, applyEdit, agenticLoop, terminal, git);
    }

    public IEditorContextProvider? GetEditorContextProvider() => _editorContextProvider;
    public IVsCommandHandler? GetCommandHandler() => _commandHandler;
}
#endif
