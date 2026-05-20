using System.Runtime.InteropServices;
using KrnlAI.VisualStudio.Commands;
using KrnlAI.VisualStudio.Options;
using KrnlAI.VisualStudio.ToolWindows;
using KrnlAI.VisualStudio.ToolWindows.Dashboard;
using KrnlAI.VisualStudio.ToolWindows.Policies;
using KrnlAI.VisualStudio.ToolWindows.Episodes;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio;

[Guid(PackageGuid)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideToolWindow(typeof(KrnlAIToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300)]
[ProvideToolWindow(typeof(DashboardToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 400)]
[ProvideToolWindow(typeof(PoliciesToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 400)]
[ProvideToolWindow(typeof(EpisodesToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, DockedWidth = 400)]
[ProvideOptionPage(typeof(KrnlAIOptionsPage), "Krnl-AI", "General", 0, 0, true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
public sealed class KrnlAIPackage : AsyncPackage
{
    public const string PackageGuid = "a6b3f8e1-2c4d-4e5f-8a9b-0c1d2e3f4a5b";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        await ShowToolWindowAsync(typeof(KrnlAIToolWindow), 0, true, cancellationToken);
        await SendSelectionToChat.InitializeAsync(this);
        await AnalyzeErrorCommand.InitializeAsync(this);
    }
}
