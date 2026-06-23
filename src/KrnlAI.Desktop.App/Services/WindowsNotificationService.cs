using System.Runtime.InteropServices;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// Gerencia notificações nativas do Windows.
/// Usa ToastService (balloon tips) como implementação principal.
/// Quando habilitado, tenta usar Windows.UI.Notifications via reflection.
/// </summary>
public sealed class WindowsNotificationService
{
    private readonly ToastService _toast;
    private bool _useActionCenter;

    public WindowsNotificationService(ToastService toast)
    {
        _toast = toast;
    }

    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        if (_useActionCenter)
        {
            TryShowActionCenter(title, message);
        }
        else
        {
            _toast.Show(title, message, type);
        }
    }

    public void EnableActionCenter()
    {
        try
        {
            var managerType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Windows.Win32, Version=10.0.0.0, Culture=neutral", false);
            _useActionCenter = managerType != null;
        }
        catch
        {
            _useActionCenter = false;
        }
    }

    private void TryShowActionCenter(string title, string message)
    {
        try
        {
            var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<toast>
  <visual>
    <binding template=""ToastGeneric"">
      <text>{title}</text>
      <text>{message}</text>
    </binding>
  </visual>
</toast>";

            var docType = Type.GetType("Windows.Data.Xml.Dom.XmlDocument, Windows.Data, Version=10.0.0.0, Culture=neutral");
            var managerType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Windows.Win32, Version=10.0.0.0, Culture=neutral");
            var notifType = Type.GetType("Windows.UI.Notifications.ToastNotification, Windows.Win32, Version=10.0.0.0, Culture=neutral");

            if (docType == null || managerType == null || notifType == null)
            {
                _toast.Show(title, message, ToastType.Info);
                return;
            }

            var doc = Activator.CreateInstance(docType);
            docType.GetMethod("LoadXml")?.Invoke(doc, [xml]);

            var toast = Activator.CreateInstance(notifType, [doc]);
            var notifier = managerType.GetMethod("CreateToastNotifier", Type.EmptyTypes)?.Invoke(null, null);
            notifier?.GetType().GetMethod("Show")?.Invoke(notifier, [toast]);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"Action Center toast failed: {ex.Message}");
            _toast.Show(title, message, ToastType.Error);
        }
    }
}
