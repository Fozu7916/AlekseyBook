using backend.Models.DTOs;

namespace backend.Services.Interfaces
{
    public interface IWallPostService
    {
        Task<WallPostDto> CreatePost(int authorId, CreateWallPostDto postDto);
        Task<WallPostDto> UpdatePost(int postId, int userId, UpdateWallPostDto postDto);
        Task DeletePost(int postId, int userId);
        Task<List<WallPostDto>> GetUserWallPosts(int wallOwnerId, int page = 1, int pageSize = 10);
        Task<WallPostDto> GetPost(int postId);
    }
} 