using KrnlAI.VisualStudio.ToolWindows.Chat;

namespace KrnlAI.VisualStudio.Services;

public sealed class ApprovalService : IApprovalService
{
    private readonly ISettingsService _settings;

    public ApprovalMode Mode => _settings.ApprovalMode;

    public ApprovalService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<ApprovalResult> RequestApprovalAsync(
        string actionDescription,
        string details,
        RiskLevel riskLevel,
        CancellationToken ct = default)
    {
        var mode = _settings.ApprovalMode;

        if (mode == Services.ApprovalMode.ChatOnly)
            return new ApprovalResult(false, "Chat-only mode: no actions executed.", null);

        var requiresApproval = mode == Services.ApprovalMode.FullApproval
            || riskLevel >= RiskLevel.High
            || mode == Services.ApprovalMode.Confirm;

        if (!requiresApproval)
            return new ApprovalResult(true, null, null);

        var app = System.Windows.Application.Current;
        if (app?.Dispatcher == null)
            return new ApprovalResult(false, "Approval dialog unavailable (no UI context).", null);

        ApprovalResult? result = null;
#pragma warning disable VSTHRD001
        await app.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new ApprovalDialog(actionDescription, details, riskLevel);
            if (app.MainWindow != null)
                dialog.Owner = app.MainWindow;
            result = dialog.ShowDialog() == true ? dialog.Result : null;
        })!;
#pragma warning restore VSTHRD001

        if (result == null)
            return new ApprovalResult(false, "Approval dialog cancelled.", null);

        return result;
    }
}
