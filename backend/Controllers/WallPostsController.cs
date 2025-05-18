using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wall-posts")]
    public class WallPostsController : ControllerBase
    {
        private readonly IWallPostService _wallPostService;
        private readonly ILogger<WallPostsController> _logger;

        public WallPostsController(IWallPostService wallPostService, ILogger<WallPostsController> logger)
        {
            _wallPostService = wallPostService;
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
        public async Task<ActionResult<WallPostDto>> CreatePost([FromBody] CreateWallPostDto postDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var post = await _wallPostService.CreatePost(userId, postDto);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании поста пользователем {UserId}", GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{postId}")]
        public async Task<ActionResult<WallPostDto>> UpdatePost(int postId, [FromBody] UpdateWallPostDto postDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var post = await _wallPostService.UpdatePost(postId, userId, postDto);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении поста {PostId} пользователем {UserId}", 
                    postId, GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{postId}")]
        public async Task<ActionResult> DeletePost(int postId)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _wallPostService.DeletePost(postId, userId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении поста {PostId} пользователем {UserId}", 
                    postId, GetCurrentUserId());
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<WallPostDto>>> GetUserWallPosts(
            int userId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var posts = await _wallPostService.GetUserWallPosts(userId, page, pageSize);
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении постов пользователя {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{postId}")]
        public async Task<ActionResult<WallPostDto>> GetPost(int postId)
        {
            try
            {
                var post = await _wallPostService.GetPost(postId);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении поста {PostId}", postId);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 