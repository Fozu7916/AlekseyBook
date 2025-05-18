using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using backend.Services.Interfaces;

namespace backend.UnitTests
{
    public class FriendsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IFriendService> _friendServiceMock;
        private readonly Mock<ILogger<FriendsController>> _loggerMock;
        private readonly FriendsController _controller;

        public FriendsControllerTests()
        {
            // Настраиваем in-memory базу данных для тестов
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestFriendsDb")
                .Options;
            _context = new ApplicationDbContext(options);

            _friendServiceMock = new Mock<IFriendService>();
            _loggerMock = new Mock<ILogger<FriendsController>>();

            _controller = new FriendsController(_friendServiceMock.Object, _loggerMock.Object);

            // Настраиваем ClaimsPrincipal для имитации авторизованного пользователя
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
        public async Task SendFriendRequest_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var friendResponse = new FriendResponseDto
            {
                Id = 1,
                User = new UserResponseDto 
                { 
                    Id = 1, 
                    Username = "testuser",
                    Email = "test@example.com",
                    Status = "Active"
                },
                Friend = new UserResponseDto 
                { 
                    Id = 2, 
                    Username = "friend",
                    Email = "friend@example.com",
                    Status = "Active"
                },
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _friendServiceMock.Setup(x => x.SendFriendRequest(1, 2))
                .ReturnsAsync(friendResponse);

            // Act
            var result = await _controller.SendFriendRequest(2);

            // Assert
            var actionResult = Assert.IsType<ActionResult<FriendResponseDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnValue = Assert.IsType<FriendResponseDto>(okResult.Value);
            Assert.Equal(2, returnValue.Friend.Id);
        }

        [Fact]
        public async Task AcceptFriendRequest_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var friendResponse = new FriendResponseDto
            {
                Id = 1,
                User = new UserResponseDto 
                { 
                    Id = 1, 
                    Username = "testuser",
                    Email = "test@example.com",
                    Status = "Active"
                },
                Friend = new UserResponseDto 
                { 
                    Id = 2, 
                    Username = "friend",
                    Email = "friend@example.com",
                    Status = "Active"
                },
                Status = "Accepted",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _friendServiceMock.Setup(x => x.AcceptFriendRequest(1, 2))
                .ReturnsAsync(friendResponse);

            // Act
            var result = await _controller.AcceptFriendRequest(2);

            // Assert
            var actionResult = Assert.IsType<ActionResult<FriendResponseDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnValue = Assert.IsType<FriendResponseDto>(okResult.Value);
            Assert.Equal("Accepted", returnValue.Status);
        }

        [Fact]
        public async Task DeclineFriendRequest_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            _friendServiceMock.Setup(x => x.DeclineFriendRequest(1, 2))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeclineFriendRequest(2);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetFriendsList_ReturnsOkResult()
        {
            // Arrange
            var friendList = new FriendListResponseDto
            {
                Friends = new List<UserResponseDto>
                {
                    new UserResponseDto 
                    { 
                        Id = 2, 
                        Username = "friend1",
                        Email = "friend1@example.com",
                        Status = "Active"
                    },
                    new UserResponseDto 
                    { 
                        Id = 3, 
                        Username = "friend2",
                        Email = "friend2@example.com",
                        Status = "Away"
                    }
                },
                PendingRequests = new List<UserResponseDto>(),
                SentRequests = new List<UserResponseDto>()
            };

            _friendServiceMock.Setup(x => x.GetFriendsList(1))
                .ReturnsAsync(friendList);

            // Act
            var result = await _controller.GetFriendsList();

            // Assert
            var actionResult = Assert.IsType<ActionResult<FriendListResponseDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnValue = Assert.IsType<FriendListResponseDto>(okResult.Value);
            Assert.Equal(2, returnValue.Friends.Count);
        }

        [Fact]
        public async Task RemoveFriend_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var friendId = 2;
            _friendServiceMock.Setup(x => x.RemoveFriend(1, friendId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveFriend(friendId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 