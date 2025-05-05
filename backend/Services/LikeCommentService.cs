using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;

namespace backend.Services
{
    public class LikeCommentService : ILikeCommentService
    {
        private readonly ApplicationDbContext _context;

        public LikeCommentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LikeDto>> GetPostLikesAsync(int postId)
        {
            var likes = await _context.Likes
                .Include(l => l.User)
                .Where(l => l.WallPostId == postId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return likes.Select(l => new LikeDto
            {
                Id = l.Id,
                User = new UserResponseDto
                {
                    Id = l.User.Id,
                    Username = l.User.Username,
                    Email = l.User.Email,
                    AvatarUrl = l.User.AvatarUrl
                },
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<LikeDto> ToggleLikeAsync(int postId, int userId)
        {
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.WallPostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return null;
            }

            var post = await _context.WallPosts.FindAsync(postId);
            if (post == null)
                return null;

            var like = new Like
            {
                WallPostId = postId,
                UserId = userId
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return new LikeDto
            {
                Id = like.Id,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl
                },
                CreatedAt = like.CreatedAt
            };
        }

        public async Task<List<CommentDto>> GetPostCommentsAsync(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.Author)
                .Where(c => c.WallPostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                Author = new UserResponseDto
                {
                    Id = c.Author.Id,
                    Username = c.Author.Username,
                    Email = c.Author.Email,
                    AvatarUrl = c.Author.AvatarUrl
                },
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<CommentDto> CreateCommentAsync(CreateCommentDto dto, int authorId)
        {
            var post = await _context.WallPosts.FindAsync(dto.WallPostId);
            if (post == null)
                return null;

            var comment = new Comment
            {
                Content = dto.Content,
                WallPostId = dto.WallPostId,
                AuthorId = authorId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var author = await _context.Users.FindAsync(authorId);

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                Author = new UserResponseDto
                {
                    Id = author.Id,
                    Username = author.Username,
                    Email = author.Email,
                    AvatarUrl = author.AvatarUrl
                },
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }

        public async Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentDto dto, int userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null || comment.AuthorId != userId)
                return null;

            comment.Content = dto.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                Author = new UserResponseDto
                {
                    Id = comment.Author.Id,
                    Username = comment.Author.Username,
                    Email = comment.Author.Email,
                    AvatarUrl = comment.Author.AvatarUrl
                },
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }

        public async Task DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null || comment.AuthorId != userId)
                return;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
} 