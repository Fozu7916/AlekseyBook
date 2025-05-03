using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, IHubContext<ChatHub> hubContext, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _hubContext = hubContext;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new Exception("Пользователь не авторизован");
            return int.Parse(userIdClaim.Value);
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto messageDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _messageService.SendMessage(userId, messageDto);

                await _hubContext.Clients.Group(messageDto.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", message);
                await _hubContext.Clients.Group(userId.ToString())
                    .SendAsync("ReceiveMessage", message);

                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отправке сообщения от пользователя {UserId} к {ReceiverId}", 
                    GetCurrentUserId(), messageDto.ReceiverId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("chat/{otherUserId}")]
        public async Task<ActionResult<List<MessageDto>>> GetChatMessages(int otherUserId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var messages = await _messageService.GetChatMessages(userId, otherUserId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при получении сообщений чата между пользователями {UserId} и {OtherUserId}", 
                    GetCurrentUserId(), otherUserId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("chats")]
        public async Task<ActionResult<List<ChatPreviewDto>>> GetUserChats()
        {
            try
            {
                var userId = GetCurrentUserId();
                var chats = await _messageService.GetUserChats(userId);
                return Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при получении списка чатов пользователя {UserId}", 
                    GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("read/{otherUserId}")]
        public async Task<ActionResult> MarkMessagesAsRead(int otherUserId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _messageService.MarkMessagesAsRead(userId, otherUserId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отметке сообщений как прочитанных между пользователями {UserId} и {OtherUserId}", 
                    GetCurrentUserId(), otherUserId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("unread/count")]
        public async Task<ActionResult<int>> GetUnreadMessagesCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _messageService.GetUnreadMessagesCount(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при получении количества непрочитанных сообщений пользователя {UserId}", 
                    GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 