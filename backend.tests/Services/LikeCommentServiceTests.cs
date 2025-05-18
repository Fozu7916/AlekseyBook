using Xunit;
using Moq;
using backend.Services;
using backend.Services.Interfaces;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend.UnitTests
{
    public class LikeCommentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<LikeCommentService>> _loggerMock;
        private readonly ILikeCommentService _likeCommentService;

        public LikeCommentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestLikeCommentServiceDb")
                .Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<LikeCommentService>>();
            _likeCommentService = new LikeCommentService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetPostLikes_ReturnsLikes()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var like = new Like
            {
                UserId = user.Id,
                User = user,
                WallPostId = post.Id,
                WallPost = post,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Likes.AddAsync(like);
            await _context.SaveChangesAsync();

            // Act
            var result = await _likeCommentService.GetPostLikesAsync(post.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(user.Id, result[0].User.Id);
        }

        [Fact]
        public async Task ToggleLike_AddLike_ReturnsLike()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            // Act
            var result = await _likeCommentService.ToggleLikeAsync(post.Id, user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.User.Id);

            var likeInDb = await _context.Likes.FirstOrDefaultAsync(l => l.UserId == user.Id && l.WallPostId == post.Id);
            Assert.NotNull(likeInDb);
        }

        [Fact]
        public async Task ToggleLike_RemoveLike_ReturnsException()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var like = new Like
            {
                UserId = user.Id,
                User = user,
                WallPostId = post.Id,
                WallPost = post,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Likes.AddAsync(like);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _likeCommentService.ToggleLikeAsync(post.Id, user.Id));
            Assert.Equal("Like removed", exception.Message);

            var likeInDb = await _context.Likes.FirstOrDefaultAsync(l => l.UserId == user.Id && l.WallPostId == post.Id);
            Assert.Null(likeInDb);
        }

        [Fact]
        public async Task GetPostComments_ReturnsComments()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var comment = new Comment
            {
                Content = "Test comment",
                AuthorId = user.Id,
                Author = user,
                WallPostId = post.Id,
                WallPost = post,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Replies = new List<Comment>()
            };
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _likeCommentService.GetPostCommentsAsync(post.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(comment.Content, result[0].Content);
            Assert.Equal(user.Id, result[0].Author.Id);
        }

        [Fact]
        public async Task CreateComment_ReturnsComment()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var createCommentDto = new CreateCommentDto
            {
                Content = "Test comment",
                WallPostId = post.Id
            };

            // Act
            var result = await _likeCommentService.CreateCommentAsync(createCommentDto, user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createCommentDto.Content, result.Content);
            Assert.Equal(user.Id, result.Author.Id);

            var commentInDb = await _context.Comments.FirstOrDefaultAsync(c => c.WallPostId == post.Id);
            Assert.NotNull(commentInDb);
            Assert.Equal(createCommentDto.Content, commentInDb.Content);
        }

        [Fact]
        public async Task UpdateComment_ReturnsUpdatedComment()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var comment = new Comment
            {
                Content = "Original comment",
                AuthorId = user.Id,
                Author = user,
                WallPostId = post.Id,
                WallPost = post,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Replies = new List<Comment>()
            };
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            var updateCommentDto = new UpdateCommentDto
            {
                Content = "Updated comment"
            };

            // Act
            var result = await _likeCommentService.UpdateCommentAsync(comment.Id, updateCommentDto, user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateCommentDto.Content, result.Content);

            var commentInDb = await _context.Comments.FindAsync(comment.Id);
            Assert.NotNull(commentInDb);
            Assert.Equal(updateCommentDto.Content, commentInDb.Content);
        }

        [Fact]
        public async Task DeleteComment_RemovesComment()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);

            var wallOwner = new User
            {
                Username = "wallowner",
                Email = "wallowner@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(wallOwner);
            await _context.SaveChangesAsync();

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = user.Id,
                Author = user,
                WallOwnerId = wallOwner.Id,
                WallOwner = wallOwner,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var comment = new Comment
            {
                Content = "Test comment",
                AuthorId = user.Id,
                Author = user,
                WallPostId = post.Id,
                WallPost = post,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Replies = new List<Comment>()
            };
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            // Act
            await _likeCommentService.DeleteCommentAsync(comment.Id, user.Id);

            // Assert
            var commentInDb = await _context.Comments.FindAsync(comment.Id);
            Assert.Null(commentInDb);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 