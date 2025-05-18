using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;
using backend.Services.Interfaces;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LikeCommentController : ControllerBase
    {
        private readonly ILikeCommentService _likeCommentService;

        public LikeCommentController(ILikeCommentService likeCommentService)
        {
            _likeCommentService = likeCommentService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new InvalidOperationException("Пользователь не авторизован");
            return int.Parse(userIdClaim.Value);
        }

        [HttpGet("posts/{postId}/likes")]
        public async Task<ActionResult<List<LikeDto>>> GetPostLikes(int postId)
        {
            var likes = await _likeCommentService.GetPostLikesAsync(postId);
            return Ok(likes);
        }

        [HttpPost("posts/{postId}/likes")]
        public async Task<ActionResult<LikeDto>> ToggleLike(int postId)
        {
            var userId = GetCurrentUserId();
            var result = await _likeCommentService.ToggleLikeAsync(postId, userId);
            
            if (result == null)
            {
                return Ok(new { message = "Лайк удален" });
            }
            
            return Ok(result);
        }

        [HttpGet("posts/{postId}/comments")]
        public async Task<ActionResult<List<CommentDto>>> GetPostComments(int postId)
        {
            var comments = await _likeCommentService.GetPostCommentsAsync(postId);
            return Ok(comments);
        }

        [HttpPost("posts/{postId}/comments")]
        public async Task<ActionResult<CommentDto>> CreateReplyComment(int postId, [FromBody] CreateCommentDto dto)
        {
            var userId = GetCurrentUserId();
            var comment = await _likeCommentService.CreateCommentAsync(dto, userId);
            return Ok(comment);
        }

        [HttpPut("comments/{commentId}")]
        public async Task<ActionResult<CommentDto>> UpdateComment(int commentId, UpdateCommentDto dto)
        {
            var userId = GetCurrentUserId();
            var comment = await _likeCommentService.UpdateCommentAsync(commentId, dto, userId);
            return Ok(comment);
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<ActionResult> DeleteComment(int commentId)
        {
            var userId = GetCurrentUserId();
            await _likeCommentService.DeleteCommentAsync(commentId, userId);
            return NoContent();
        }
    }
} 