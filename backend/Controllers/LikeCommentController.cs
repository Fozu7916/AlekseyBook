using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Services;
using backend.Models.DTOs;
using System.Security.Claims;

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
        public async Task<List<LikeDto>> GetPostLikes(int postId)
        {
            return await _likeCommentService.GetPostLikesAsync(postId);
        }

        [HttpPost("posts/{postId}/likes")]
        public async Task<LikeDto> ToggleLike(int postId)
        {
            var userId = GetCurrentUserId();
            return await _likeCommentService.ToggleLikeAsync(postId, userId);
        }

        [HttpGet("posts/{postId}/comments")]
        public async Task<List<CommentDto>> GetPostComments(int postId)
        {
            return await _likeCommentService.GetPostCommentsAsync(postId);
        }

        [HttpPost("comments")]
        public async Task<CommentDto> CreateComment(CreateCommentDto dto)
        {
            var userId = GetCurrentUserId();
            return await _likeCommentService.CreateCommentAsync(dto, userId);
        }

        [HttpPut("comments/{commentId}")]
        public async Task<CommentDto> UpdateComment(int commentId, UpdateCommentDto dto)
        {
            var userId = GetCurrentUserId();
            return await _likeCommentService.UpdateCommentAsync(commentId, dto, userId);
        }

        [HttpDelete("comments/{commentId}")]
        public async Task DeleteComment(int commentId)
        {
            var userId = GetCurrentUserId();
            await _likeCommentService.DeleteCommentAsync(commentId, userId);
        }
    }
} 