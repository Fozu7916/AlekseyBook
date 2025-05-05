using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;

namespace backend.Services
{
    public class WallPostService : IWallPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WallPostService> _logger;

        public WallPostService(ApplicationDbContext context, ILogger<WallPostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WallPostDto> CreatePost(int authorId, CreateWallPostDto postDto)
        {
            var wallPost = new WallPost
            {
                Content = postDto.Content,
                ImageUrl = postDto.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                AuthorId = authorId,
                WallOwnerId = postDto.WallOwnerId,
                IsDeleted = false
            };

            _context.WallPosts.Add(wallPost);
            await _context.SaveChangesAsync();

            return await GetPostDto(wallPost.Id);
        }

        public async Task<WallPostDto> UpdatePost(int postId, int userId, UpdateWallPostDto postDto)
        {
            var post = await _context.WallPosts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null)
                throw new Exception("Пост не найден");

            if (post.AuthorId != userId)
                throw new Exception("У вас нет прав на редактирование этого поста");

            post.Content = postDto.Content;
            post.ImageUrl = postDto.ImageUrl;

            await _context.SaveChangesAsync();

            return await GetPostDto(post.Id);
        }

        public async Task DeletePost(int postId, int userId)
        {
            var post = await _context.WallPosts
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                throw new Exception("Пост не найден");

            if (post.IsDeleted)
                throw new Exception("Пост уже удален");

            if (post.AuthorId != userId && post.WallOwnerId != userId)
                throw new Exception("У вас нет прав на удаление этого поста");

            post.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task<List<WallPostDto>> GetUserWallPosts(int wallOwnerId, int page = 1, int pageSize = 10)
        {
            var posts = await _context.WallPosts
                .Where(p => p.WallOwnerId == wallOwnerId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new WallPostDto
                {
                    Id = p.Id,
                    Content = p.Content,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt,
                    AuthorId = p.AuthorId,
                    AuthorName = p.Author.Username,
                    AuthorAvatarUrl = p.Author.AvatarUrl,
                    WallOwnerId = p.WallOwnerId
                })
                .ToListAsync();

            return posts;
        }

        public async Task<WallPostDto> GetPost(int postId)
        {
            return await GetPostDto(postId);
        }

        private async Task<WallPostDto> GetPostDto(int postId)
        {
            var post = await _context.WallPosts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null)
                throw new Exception("Пост не найден");

            return new WallPostDto
            {
                Id = post.Id,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                AuthorId = post.AuthorId,
                AuthorName = post.Author.Username,
                AuthorAvatarUrl = post.Author.AvatarUrl,
                WallOwnerId = post.WallOwnerId
            };
        }
    }
} 