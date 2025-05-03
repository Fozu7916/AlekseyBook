using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/friends")]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly ILogger<FriendsController> _logger;

        public FriendsController(IFriendService friendService, ILogger<FriendsController> logger)
        {
            _friendService = friendService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new Exception("Пользователь не авторизован");
            
            return int.Parse(userIdClaim.Value);
        }

        [HttpPost("{friendId}")]
        public async Task<ActionResult<FriendResponseDto>> SendFriendRequest(int friendId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendService.SendFriendRequest(userId, friendId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отправке запроса в друзья от пользователя {UserId} к {FriendId}", 
                    GetCurrentUserId(), friendId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{friendId}/accept")]
        public async Task<ActionResult<FriendResponseDto>> AcceptFriendRequest(int friendId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendService.AcceptFriendRequest(userId, friendId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при принятии запроса в друзья от пользователя {FriendId} пользователем {UserId}", 
                    friendId, GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{friendId}/decline")]
        public async Task<ActionResult> DeclineFriendRequest(int friendId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendService.DeclineFriendRequest(userId, friendId);
                if (!result)
                    return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при отклонении запроса в друзья от пользователя {FriendId} пользователем {UserId}", 
                    friendId, GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{friendId}")]
        public async Task<ActionResult> RemoveFriend(int friendId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendService.RemoveFriend(userId, friendId);
                if (!result)
                    return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при удалении из друзей пользователя {FriendId} пользователем {UserId}", 
                    friendId, GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{userId}/block")]
        public async Task<ActionResult> BlockUser(int userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _friendService.BlockUser(currentUserId, userId);
                if (!result)
                    return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при блокировке пользователя {BlockedUserId} пользователем {UserId}", 
                    userId, GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<FriendListResponseDto>> GetFriendsList()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendService.GetFriendsList(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при получении списка друзей пользователя {UserId}", 
                    GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<UserResponseDto>>> GetUserFriendsList(int userId)
        {
            try
            {
                var result = await _friendService.GetUserFriendsList(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при получении списка друзей пользователя {TargetUserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{friendId}/status")]
        public async Task<ActionResult<bool>> CheckFriendshipStatus(int friendId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _friendService.IsFriend(userId, friendId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при проверке статуса дружбы между пользователями {UserId} и {FriendId}", 
                    GetCurrentUserId(), friendId);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 