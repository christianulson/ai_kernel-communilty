using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface INotificationClient
{
    Task<List<NotificationItem>> GetNotificationsAsync(CancellationToken ct = default);
    Task<bool> MarkNotificationReadAsync(string notificationId, CancellationToken ct = default);
}
