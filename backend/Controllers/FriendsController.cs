using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/friends")]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendsController(IFriendService friendService)
        {
            _friendService = friendService;
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
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 