using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.Extensions.Logging;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class LikeCommentService : ILikeCommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LikeCommentService> _logger;

        public LikeCommentService(ApplicationDbContext context, ILogger<LikeCommentService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    AvatarUrl = l.User.AvatarUrl ?? string.Empty
                },
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<LikeDto?> ToggleLikeAsync(int postId, int userId)
        {
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.WallPostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return null;
            }

            var post = await _context.WallPosts.FindAsync(postId) 
                ?? throw new Exception("Post not found");

            var user = await _context.Users.FindAsync(userId)
                ?? throw new Exception("User not found");

            var like = new Like
            {
                WallPostId = postId,
                UserId = userId,
                WallPost = post,
                User = user
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return new LikeDto
            {
                Id = like.Id,
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl ?? string.Empty
                },
                CreatedAt = like.CreatedAt
            };
        }

        public async Task<List<CommentDto>> GetPostCommentsAsync(int postId)
        {
            var comments = await _context.Comments
                .AsNoTracking()
                .Include(c => c.Author)
                .Where(c => c.WallPostId == postId)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .ToListAsync();

            var firstComment = await _context.Comments
                .FirstOrDefaultAsync(c => c.WallPostId == postId);

            var userId = firstComment?.AuthorId;

            var commentLikes = userId.HasValue
                ? await _context.CommentLikes
                    .Where(cl => cl.UserId == userId && comments.Select(c => c.Id).Contains(cl.CommentId))
                    .ToListAsync()
                : new List<CommentLike>();

            return comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                Author = new UserResponseDto
                {
                    Id = c.Author.Id,
                    Username = c.Author.Username,
                    Email = c.Author.Email,
                    AvatarUrl = c.Author.AvatarUrl ?? string.Empty
                },
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ParentId = c.ParentId,
                Likes = _context.CommentLikes.Count(cl => cl.CommentId == c.Id),
                IsLiked = commentLikes.Any(cl => cl.CommentId == c.Id)
            }).ToList();
        }

        public async Task<CommentDto> CreateCommentAsync(CreateCommentDto dto, int authorId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var post = await _context.WallPosts.FindAsync(dto.WallPostId)
                ?? throw new Exception("Post not found");

            if (dto.ParentId.HasValue)
            {
                var parentComment = await _context.Comments.FindAsync(dto.ParentId.Value);
                if (parentComment == null || parentComment.WallPostId != dto.WallPostId)
                {
                    throw new Exception("Parent comment not found or belongs to different post");
                }
            }

            var author = await _context.Users.FindAsync(authorId)
                ?? throw new Exception("User not found");

            var comment = new Comment
            {
                Content = dto.Content,
                WallPostId = dto.WallPostId,
                AuthorId = authorId,
                ParentId = dto.ParentId,
                Author = author,
                WallPost = post,
                Replies = new List<Comment>()
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                Author = new UserResponseDto
                {
                    Id = author.Id,
                    Username = author.Username,
                    Email = author.Email,
                    AvatarUrl = author.AvatarUrl ?? string.Empty
                },
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                ParentId = comment.ParentId
            };
        }

        public async Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentDto dto, int userId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var comment = await _context.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == commentId)
                ?? throw new Exception("Comment not found");

            if (comment.AuthorId != userId)
            {
                throw new Exception("You don't have permission to update this comment");
            }

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
                    AvatarUrl = comment.Author.AvatarUrl ?? string.Empty
                },
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                ParentId = comment.ParentId
            };
        }

        public async Task DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId)
                ?? throw new Exception("Comment not found");

            if (comment.AuthorId != userId)
            {
                throw new Exception("You don't have permission to delete this comment");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
} 