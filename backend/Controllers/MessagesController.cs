using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(IMessageService messageService, IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _hubContext = hubContext;
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
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 