using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.App.Services;

public sealed class WindowsNotificationService
{
    private readonly ToastService _toast;

    public WindowsNotificationService(ToastService toast)
    {
        _toast = toast;
    }

    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        _toast.Show(title, message, type);
    }
}
