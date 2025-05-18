using backend.Models.DTOs;

namespace backend.Services.Interfaces
{
    public interface ILikeCommentService
    {
        Task<List<LikeDto>> GetPostLikesAsync(int postId);
        Task<LikeDto?> ToggleLikeAsync(int postId, int userId);
        Task<List<CommentDto>> GetPostCommentsAsync(int postId);
        Task<CommentDto> CreateCommentAsync(CreateCommentDto dto, int authorId);
        Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentDto dto, int userId);
        Task DeleteCommentAsync(int commentId, int userId);
    }
} 