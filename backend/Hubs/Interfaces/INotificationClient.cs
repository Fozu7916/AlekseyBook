using backend.Models.DTOs;

namespace backend.Hubs.Interfaces
{
    public interface INotificationClient
    {
        Task ReceiveNotification(NotificationDto notification);
        Task NotificationRead(int notificationId);
        Task AllNotificationsRead();
        Task NotificationDeleted(int notificationId);
    }
} 