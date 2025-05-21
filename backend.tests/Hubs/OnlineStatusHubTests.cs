using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using backend.Hubs;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace backend.UnitTests.Hubs
{
    public class OnlineStatusHubTests
    {
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly Mock<ILogger<OnlineStatusHub>> _mockLogger;
        private readonly ApplicationDbContext _context;
        private readonly OnlineStatusHub _hub;

        public OnlineStatusHubTests()
        {
            // Настройка базы данных
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Настройка моков
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();
            _mockContext = new Mock<HubCallerContext>();
            _mockLogger = new Mock<ILogger<OnlineStatusHub>>();

            // Настройка hub
            _hub = new OnlineStatusHub(_context, _mockLogger.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            // Общие настройки моков
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);
        }

        [Fact]
        public async Task OnConnectedAsync_AuthenticatedUser_UpdatesOnlineStatus()
        {
            // Arrange
            var userId = "1";
            var user = new User { 
                Id = 1, 
                Username = "testUser", 
                IsOnline = false,
                Email = "test@test.com",
                PasswordHash = "hash123"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(claimsPrincipal);
            _mockContext.Setup(c => c.ConnectionId).Returns("connection1");

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.IsOnline);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "UserOnlineStatusChanged",
                    It.Is<object[]>(args => 
                        args != null && 
                        args.Length == 1 && 
                        VerifyUserStatusObject(args[0], 1, true)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_LastConnection_UpdatesOfflineStatus()
        {
            // Arrange
            var userId = "1";
            var user = new User { 
                Id = 1, 
                Username = "testUser", 
                IsOnline = true,
                Email = "test@test.com",
                PasswordHash = "hash123"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(claimsPrincipal);
            _mockContext.Setup(c => c.ConnectionId).Returns("connection1");

            // Сначала подключаем пользователя
            await _hub.OnConnectedAsync();

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            var updatedUser = await _context.Users.FindAsync(1);
            Assert.NotNull(updatedUser);
            Assert.False(updatedUser.IsOnline);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "UserOnlineStatusChanged",
                    It.Is<object[]>(args => 
                        args != null && 
                        args.Length == 1 && 
                        VerifyUserStatusObject(args[0], 1, false)),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetOnlineUsers_ReturnsOnlineUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = 1, Username = "user1", IsOnline = true, Email = "user1@test.com", PasswordHash = "hash1" },
                new User { Id = 2, Username = "user2", IsOnline = false, Email = "user2@test.com", PasswordHash = "hash2" },
                new User { Id = 3, Username = "user3", IsOnline = true, Email = "user3@test.com", PasswordHash = "hash3" }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            await _hub.GetOnlineUsers();

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "OnlineUsersReceived",
                    It.Is<object[]>(args => 
                        args != null && 
                        args.Length == 1 && 
                        ((IEnumerable<object>)args[0]).Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_UnauthenticatedUser_ThrowsException()
        {
            // Arrange
            _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);

            // Act & Assert
            await Assert.ThrowsAsync<HubException>(() => _hub.OnConnectedAsync());
        }

        private static bool VerifyUserStatusObject(object obj, int expectedUserId, bool expectedIsOnline)
        {
            if (obj == null) return false;
            var userId = Convert.ToInt32(GetPropertyValue(obj, "UserId"));
            var isOnline = Convert.ToBoolean(GetPropertyValue(obj, "IsOnline"));
            return userId == expectedUserId && isOnline == expectedIsOnline;
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            var value = obj.GetType().GetProperty(propertyName)?.GetValue(obj);
            if (value == null)
            {
                throw new ArgumentException($"Property {propertyName} not found or value is null");
            }
            return value;
        }
    }
} 