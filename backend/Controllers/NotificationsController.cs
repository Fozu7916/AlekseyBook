using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services.Interfaces;
using backend.Models.DTOs;
using System.Security.Claims;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new Exception("Пользователь не авторизован");
            return int.Parse(userIdClaim.Value);
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotifications(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении уведомлений пользователя {UserId}", GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("unread/count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.GetUnreadNotificationsCount(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении количества непрочитанных уведомлений пользователя {UserId}", GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAsRead(userId, id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отметке уведомления как прочитанного. UserId: {UserId}, NotificationId: {NotificationId}", 
                    GetCurrentUserId(), id);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAllAsRead(userId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отметке всех уведомлений как прочитанных. UserId: {UserId}", GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.DeleteNotification(userId, id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении уведомления. UserId: {UserId}, NotificationId: {NotificationId}", 
                    GetCurrentUserId(), id);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 