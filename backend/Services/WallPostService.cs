using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class WallPostService : IWallPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WallPostService> _logger;

        public WallPostService(ApplicationDbContext context, ILogger<WallPostService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WallPostDto> CreatePost(int authorId, CreateWallPostDto postDto)
        {
            if (postDto == null)
                throw new ArgumentNullException(nameof(postDto));

            var author = await _context.Users.FindAsync(authorId) 
                ?? throw new Exception("Автор не найден");
            
            var wallOwner = await _context.Users.FindAsync(postDto.WallOwnerId)
                ?? throw new Exception("Владелец стены не найден");

            var wallPost = new WallPost
            {
                Content = postDto.Content,
                ImageUrl = postDto.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = authorId,
                Author = author,
                WallOwnerId = postDto.WallOwnerId,
                WallOwner = wallOwner,
                IsDeleted = false,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };

            _context.WallPosts.Add(wallPost);
            await _context.SaveChangesAsync();

            return await GetPostDto(wallPost.Id);
        }

        public async Task<WallPostDto> UpdatePost(int postId, int userId, UpdateWallPostDto postDto)
        {
            if (postDto == null)
                throw new ArgumentNullException(nameof(postDto));

            var post = await _context.WallPosts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted)
                ?? throw new Exception("Пост не найден");

            if (post.AuthorId != userId)
                throw new Exception("У вас нет прав на редактирование этого поста");

            post.Content = postDto.Content;
            post.ImageUrl = postDto.ImageUrl;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetPostDto(post.Id);
        }

        public async Task DeletePost(int postId, int userId)
        {
            var post = await _context.WallPosts
                .FirstOrDefaultAsync(p => p.Id == postId)
                ?? throw new Exception("Пост не найден");

            if (post.IsDeleted)
                throw new Exception("Пост уже удален");

            if (post.AuthorId != userId && post.WallOwnerId != userId)
                throw new Exception("У вас нет прав на удаление этого поста");

            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<List<WallPostDto>> GetUserWallPosts(int wallOwnerId, int page = 1, int pageSize = 10)
        {
            var posts = await _context.WallPosts
                .Include(p => p.Author)
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
                    AuthorAvatarUrl = p.Author.AvatarUrl ?? string.Empty,
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
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted)
                ?? throw new Exception("Пост не найден");

            return new WallPostDto
            {
                Id = post.Id,
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                AuthorId = post.AuthorId,
                AuthorName = post.Author.Username,
                AuthorAvatarUrl = post.Author.AvatarUrl ?? string.Empty,
                WallOwnerId = post.WallOwnerId
            };
        }
    }
} 