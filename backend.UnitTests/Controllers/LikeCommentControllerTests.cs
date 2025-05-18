using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using backend.Services.Interfaces;

namespace backend.UnitTests.Controllers
{
    public class LikeCommentControllerTests
    {
        private readonly Mock<ILikeCommentService> _likeCommentServiceMock;
        private readonly LikeCommentController _controller;

        public LikeCommentControllerTests()
        {
            _likeCommentServiceMock = new Mock<ILikeCommentService>();
            _controller = new LikeCommentController(_likeCommentServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task CreateReplyComment_ValidData_ReturnsComment()
        {
            // Arrange
            var commentDto = new CreateCommentDto
            {
                WallPostId = 1,
                Content = "Test comment"
            };

            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active"
            };

            var commentResponse = new CommentDto
            {
                Id = 1,
                Content = commentDto.Content,
                CreatedAt = DateTime.UtcNow,
                Author = userResponse
            };

            _likeCommentServiceMock.Setup(x => x.CreateCommentAsync(It.IsAny<CreateCommentDto>(), 1))
                .ReturnsAsync(commentResponse);

            // Act
            var result = await _controller.CreateReplyComment(1, commentDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<CommentDto>(okResult.Value);
            Assert.Equal(commentDto.Content, returnValue.Content);
            Assert.Equal(userResponse.Id, returnValue.Author.Id);
            Assert.Equal(userResponse.Username, returnValue.Author.Username);
        }

        [Fact]
        public async Task ToggleLike_ValidData_ReturnsLike()
        {
            // Arrange
            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active"
            };

            var likeResponse = new LikeDto
            {
                Id = 1,
                CreatedAt = DateTime.UtcNow,
                User = userResponse
            };

            _likeCommentServiceMock.Setup(x => x.ToggleLikeAsync(1, 1))
                .ReturnsAsync(likeResponse);

            // Act
            var result = await _controller.ToggleLike(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<LikeDto>(okResult.Value);
            Assert.Equal(userResponse.Id, returnValue.User.Id);
        }

        [Fact]
        public async Task ToggleLike_RemovesLike_ReturnsOkResult()
        {
            // Arrange
            _likeCommentServiceMock
                .Setup(x => x.ToggleLikeAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((LikeDto?)null);

            // Act
            var result = await _controller.ToggleLike(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
            Assert.True(jsonElement.TryGetProperty("message", out var messageElement));
            var message = messageElement.GetString();
            Assert.NotNull(message);
            Assert.Equal("Лайк удален", message);
        }

        [Fact]
        public async Task GetPostComments_ReturnsCommentsList()
        {
            // Arrange
            var userResponse1 = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active"
            };

            var userResponse2 = new UserResponseDto
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Status = "Active"
            };

            var comments = new List<CommentDto>
            {
                new CommentDto
                {
                    Id = 1,
                    Content = "Comment 1",
                    CreatedAt = DateTime.UtcNow,
                    Author = userResponse1
                },
                new CommentDto
                {
                    Id = 2,
                    Content = "Comment 2",
                    CreatedAt = DateTime.UtcNow,
                    Author = userResponse2
                }
            };

            _likeCommentServiceMock.Setup(x => x.GetPostCommentsAsync(1))
                .ReturnsAsync(comments);

            // Act
            var result = await _controller.GetPostComments(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<CommentDto>>(okResult.Value);
            Assert.NotNull(returnValue);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal("Comment 1", returnValue[0].Content);
            Assert.Equal("Comment 2", returnValue[1].Content);
        }

        [Fact]
        public async Task GetPostLikes_ReturnsLikesList()
        {
            // Arrange
            var userResponse1 = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active"
            };

            var userResponse2 = new UserResponseDto
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Status = "Active"
            };

            var likes = new List<LikeDto>
            {
                new LikeDto
                {
                    Id = 1,
                    CreatedAt = DateTime.UtcNow,
                    User = userResponse1
                },
                new LikeDto
                {
                    Id = 2,
                    CreatedAt = DateTime.UtcNow,
                    User = userResponse2
                }
            };

            _likeCommentServiceMock.Setup(x => x.GetPostLikesAsync(1))
                .ReturnsAsync(likes);

            // Act
            var result = await _controller.GetPostLikes(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<LikeDto>>(okResult.Value);
            Assert.NotNull(returnValue);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal(1, returnValue[0].User.Id);
            Assert.Equal(2, returnValue[1].User.Id);
        }
    }
} 