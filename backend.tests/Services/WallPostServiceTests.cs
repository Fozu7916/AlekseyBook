using Xunit;
using Moq;
using backend.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace backend.UnitTests.Services
{
    public class WallPostServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<WallPostService>> _loggerMock;
        private readonly WallPostService _wallPostService;

        public WallPostServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestWallPostServiceDb")
                .Options;
            _context = new ApplicationDbContext(options);

            _loggerMock = new Mock<ILogger<WallPostService>>();

            _wallPostService = new WallPostService(
                _context,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task CreatePost_ValidData_ReturnsPost()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active",
                PasswordHash = "hash"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var createPostDto = new CreateWallPostDto
            {
                Content = "Test post content",
                WallOwnerId = userId
            };

            // Act
            var result = await _wallPostService.CreatePost(userId, createPostDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createPostDto.Content, result.Content);
            Assert.Equal(userId, result.AuthorId);
            Assert.Equal(user.Username, result.AuthorName);

            var postInDb = await _context.WallPosts.FirstOrDefaultAsync(p => p.AuthorId == userId);
            Assert.NotNull(postInDb);
            Assert.Equal(createPostDto.Content, postInDb.Content);
        }

        [Fact]
        public async Task GetUserWallPosts_ReturnsPostsList()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active",
                PasswordHash = "hash"
            };
            await _context.Users.AddAsync(user);

            var posts = new List<WallPost>
            {
                new WallPost
                {
                    Content = "Test post 1",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AuthorId = userId,
                    Author = user,
                    WallOwnerId = userId,
                    WallOwner = user,
                    Likes = new List<Like>(),
                    Comments = new List<Comment>()
                },
                new WallPost
                {
                    Content = "Test post 2",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AuthorId = userId,
                    Author = user,
                    WallOwnerId = userId,
                    WallOwner = user,
                    Likes = new List<Like>(),
                    Comments = new List<Comment>()
                }
            };
            await _context.WallPosts.AddRangeAsync(posts);
            await _context.SaveChangesAsync();

            // Act
            var result = await _wallPostService.GetUserWallPosts(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Test post 2", result.First().Content);
            Assert.All(result, post =>
            {
                Assert.Equal(userId, post.AuthorId);
                Assert.Equal(user.Username, post.AuthorName);
            });
        }

        [Fact]
        public async Task UpdatePost_ValidData_ReturnsUpdatedPost()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active",
                PasswordHash = "hash"
            };
            await _context.Users.AddAsync(user);

            var post = new WallPost
            {
                Content = "Original content",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = userId,
                Author = user,
                WallOwnerId = userId,
                WallOwner = user,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            var updatePostDto = new UpdateWallPostDto
            {
                Content = "Updated content"
            };

            // Act
            var result = await _wallPostService.UpdatePost(post.Id, userId, updatePostDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updatePostDto.Content, result.Content);
            Assert.Equal(userId, result.AuthorId);
            Assert.Equal(user.Username, result.AuthorName);

            var postInDb = await _context.WallPosts.FindAsync(post.Id);
            Assert.NotNull(postInDb);
            Assert.Equal(updatePostDto.Content, postInDb.Content);
        }

        [Fact]
        public async Task DeletePost_ValidRequest_DeletesPost()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active",
                PasswordHash = "hash"
            };
            await _context.Users.AddAsync(user);

            var post = new WallPost
            {
                Content = "Test post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = userId,
                Author = user,
                WallOwnerId = userId,
                WallOwner = user,
                Likes = new List<Like>(),
                Comments = new List<Comment>()
            };
            await _context.WallPosts.AddAsync(post);
            await _context.SaveChangesAsync();

            // Act
            await _wallPostService.DeletePost(post.Id, userId);

            // Assert
            var postInDb = await _context.WallPosts.FindAsync(post.Id);
            Assert.NotNull(postInDb);
            Assert.True(postInDb.IsDeleted);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 