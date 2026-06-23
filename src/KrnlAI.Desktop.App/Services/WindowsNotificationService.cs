using System.Security;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Services;

public sealed class WindowsNotificationService
{
    private readonly ToastService _toast;
    private bool _actionCenterAvailable;
    private bool _actionCenterChecked;
    private static readonly object CheckLock = new();

    public WindowsNotificationService(ToastService toast)
    {
        _toast = toast;
    }

    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        if (TryGetActionCenter() && TryShowActionCenter(title, message))
            return;

        _toast.Show(title, message, type);
    }

    private bool TryGetActionCenter()
    {
        if (_actionCenterChecked) return _actionCenterAvailable;
        lock (CheckLock)
        {
            if (_actionCenterChecked) return _actionCenterAvailable;
            try
            {
                var managerType = Type.GetType(
                    "Windows.UI.Notifications.ToastNotificationManager, Windows.Win32, Version=10.0.0.0, Culture=neutral",
                    false);
                _actionCenterAvailable = managerType != null;
            }
            catch
            {
                _actionCenterAvailable = false;
            }
            _actionCenterChecked = true;
        }
        return _actionCenterAvailable;
    }

    private bool TryShowActionCenter(string title, string message)
    {
        try
        {
            var safeTitle = SecurityElement.Escape(title);
            var safeMessage = SecurityElement.Escape(message);

            var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<toast>
  <visual>
    <binding template=""ToastGeneric"">
      <text>{safeTitle}</text>
      <text>{safeMessage}</text>
    </binding>
  </visual>
</toast>";

            var docType = Type.GetType("Windows.Data.Xml.Dom.XmlDocument, Windows.Data, Version=10.0.0.0, Culture=neutral", false);
            var managerType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Windows.Win32, Version=10.0.0.0, Culture=neutral", false);
            var notifType = Type.GetType("Windows.UI.Notifications.ToastNotification, Windows.Win32, Version=10.0.0.0, Culture=neutral", false);

            if (docType == null || managerType == null || notifType == null)
                return false;

            var doc = Activator.CreateInstance(docType);
            docType.GetMethod("LoadXml")?.Invoke(doc, [xml]);

            var toast = Activator.CreateInstance(notifType, [doc]);
            var notifier = managerType.GetMethod("CreateToastNotifier", Type.EmptyTypes)?.Invoke(null, null);
            notifier?.GetType().GetMethod("Show")?.Invoke(notifier, [toast]);

            return true;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write($"Native toast failed: {ex.Message}");
            return false;
        }
    }
}
