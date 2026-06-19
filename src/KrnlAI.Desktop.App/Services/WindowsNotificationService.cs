namespace KrnlAI.Desktop.App.Services;

public sealed class WindowsNotificationService(ToastService toast)
{
    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        toast.Show(title, message, type);
    }
}
