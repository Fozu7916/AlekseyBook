using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using backend.Hubs.Interfaces;

namespace backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public NotificationService(
            ApplicationDbContext context, 
            IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<NotificationDto> CreateNotification(CreateNotificationDto notificationDto)
        {
            var notification = new Notification
            {
                UserId = notificationDto.UserId,
                Type = Enum.Parse<NotificationType>(notificationDto.Type),
                Title = notificationDto.Title,
                Text = notificationDto.Text,
                Link = notificationDto.Link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                User = await _context.Users.FindAsync(notificationDto.UserId) 
                    ?? throw new InvalidOperationException($"User not found: {notificationDto.UserId}")
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var notificationResponse = MapToDto(notification);
            await _hubContext.Clients.Group(notification.UserId.ToString())
                .ReceiveNotification(notificationResponse);

            return notificationResponse;
        }

        public async Task<List<NotificationDto>> GetUserNotifications(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return notifications.Select(MapToDto).ToList();
        }

        public async Task<int> GetUnreadNotificationsCount(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsRead(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
                ?? throw new InvalidOperationException($"Notification not found: {notificationId}");

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(userId.ToString())
                .NotificationRead(notificationId);
        }

        public async Task MarkAllAsRead(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(userId.ToString())
                .AllNotificationsRead();
        }

        public async Task DeleteNotification(int userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
                ?? throw new InvalidOperationException($"Notification not found: {notificationId}");

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(userId.ToString())
                .NotificationDeleted(notificationId);
        }

        private static NotificationDto MapToDto(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type.ToString(),
                Title = notification.Title,
                Text = notification.Text,
                Link = notification.Link,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }
} 