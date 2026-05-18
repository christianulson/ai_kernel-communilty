using System.Runtime.InteropServices;
using KrnlAI.VisualStudio.Commands;
using KrnlAI.VisualStudio.Options;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio;

[Guid(PackageGuid)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideToolWindow(typeof(ToolWindows.KrnlAIToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300)]
[ProvideOptionPage(typeof(KrnlAIOptionsPage), "Krnl-AI", "General", 0, 0, true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
public sealed class KrnlAIPackage : AsyncPackage
{
    public const string PackageGuid = "a6b3f8e1-2c4d-4e5f-8a9b-0c1d2e3f4a5b";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        await ShowToolWindowAsync(typeof(ToolWindows.KrnlAIToolWindow), 0, true, cancellationToken);
        await SendSelectionToChat.InitializeAsync(this);
        await AnalyzeErrorCommand.InitializeAsync(this);
    }
}
