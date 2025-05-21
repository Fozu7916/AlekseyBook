using backend.Models.DTOs;

namespace backend.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateNotification(CreateNotificationDto notificationDto);
        Task<List<NotificationDto>> GetUserNotifications(int userId);
        Task<int> GetUnreadNotificationsCount(int userId);
        Task MarkAsRead(int userId, int notificationId);
        Task MarkAllAsRead(int userId);
        Task DeleteNotification(int userId, int notificationId);
    }
} 