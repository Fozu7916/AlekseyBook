using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Models.DTOs;
using backend.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;

namespace backend.UnitTests
{
    public class WallPostsControllerTests
    {
        private readonly Mock<IWallPostService> _wallPostServiceMock;
        private readonly Mock<ILogger<WallPostsController>> _loggerMock;
        private readonly WallPostsController _controller;

        public WallPostsControllerTests()
        {
            _wallPostServiceMock = new Mock<IWallPostService>();
            _loggerMock = new Mock<ILogger<WallPostsController>>();

            _controller = new WallPostsController(_wallPostServiceMock.Object, _loggerMock.Object);

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
        public async Task CreatePost_ValidData_ReturnsPost()
        {
            // Arrange
            var createPostDto = new CreateWallPostDto
            {
                Content = "Test post content"
            };

            var postResponse = new WallPostDto
            {
                Id = 1,
                Content = createPostDto.Content,
                CreatedAt = DateTime.UtcNow,
                AuthorId = 1,
                AuthorName = "testuser",
                AuthorAvatarUrl = "test.jpg"
            };

            _wallPostServiceMock.Setup(x => x.CreatePost(1, createPostDto))
                .ReturnsAsync(postResponse);

            // Act
            var result = await _controller.CreatePost(createPostDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<WallPostDto>>(result);
            Assert.NotNull(actionResult.Value);
            var returnValue = actionResult.Value;
            Assert.NotNull(returnValue.Content);
            Assert.NotNull(returnValue.AuthorName);
            Assert.Equal(createPostDto.Content, returnValue.Content);
            Assert.Equal(1, returnValue.AuthorId);
            Assert.Equal("testuser", returnValue.AuthorName);
        }

        [Fact]
        public async Task CreatePost_InvalidData_ReturnsBadRequest()
        {
            // Arrange
            var createPostDto = new CreateWallPostDto
            {
                Content = "Test post content"
            };

            _wallPostServiceMock.Setup(x => x.CreatePost(1, createPostDto))
                .ThrowsAsync(new Exception("Ошибка при создании поста"));

            // Act
            var result = await _controller.CreatePost(createPostDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<WallPostDto>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при создании поста", errorMessage);
        }

        [Fact]
        public async Task GetUserWallPosts_ReturnsPostsList()
        {
            // Arrange
            var posts = new List<WallPostDto>
            {
                new WallPostDto
                {
                    Id = 1,
                    Content = "Test post 1",
                    CreatedAt = DateTime.UtcNow,
                    AuthorId = 1,
                    AuthorName = "testuser",
                    AuthorAvatarUrl = "test1.jpg"
                },
                new WallPostDto
                {
                    Id = 2,
                    Content = "Test post 2",
                    CreatedAt = DateTime.UtcNow,
                    AuthorId = 1,
                    AuthorName = "testuser",
                    AuthorAvatarUrl = "test2.jpg"
                }
            };

            _wallPostServiceMock.Setup(x => x.GetUserWallPosts(1, 1, 10))
                .ReturnsAsync(posts);

            // Act
            var result = await _controller.GetUserWallPosts(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<WallPostDto>>>(result);
            var returnValue = Assert.IsType<List<WallPostDto>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal("Test post 1", returnValue[0].Content);
            Assert.Equal("Test post 2", returnValue[1].Content);
        }

        [Fact]
        public async Task UpdatePost_ValidData_ReturnsUpdatedPost()
        {
            // Arrange
            var updatePostDto = new UpdateWallPostDto
            {
                Content = "Updated content"
            };

            var updatedPost = new WallPostDto
            {
                Id = 1,
                Content = updatePostDto.Content,
                CreatedAt = DateTime.UtcNow,
                AuthorId = 1,
                AuthorName = "testuser",
                AuthorAvatarUrl = "test.jpg"
            };

            _wallPostServiceMock.Setup(x => x.UpdatePost(1, 1, updatePostDto))
                .ReturnsAsync(updatedPost);

            // Act
            var result = await _controller.UpdatePost(1, updatePostDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<WallPostDto>>(result);
            Assert.NotNull(actionResult.Value);
            var returnValue = actionResult.Value;
            Assert.NotNull(returnValue.Content);
            Assert.NotNull(returnValue.AuthorName);
            Assert.Equal(updatePostDto.Content, returnValue.Content);
            Assert.Equal(1, returnValue.AuthorId);
            Assert.Equal("testuser", returnValue.AuthorName);
        }

        [Fact]
        public async Task DeletePost_ValidRequest_ReturnsOk()
        {
            // Arrange
            _wallPostServiceMock.Setup(x => x.DeletePost(1, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeletePost(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var successObj = okResult.Value;
            Assert.Equal(true, successObj.GetType().GetProperty("success").GetValue(successObj));
        }

        [Fact]
        public async Task DeletePost_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            _wallPostServiceMock.Setup(x => x.DeletePost(1, 1))
                .ThrowsAsync(new Exception("Ошибка при удалении поста"));

            // Act
            var result = await _controller.DeletePost(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при удалении поста", errorMessage);
        }
    }
} 