using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using backend.Hubs;
using Microsoft.AspNetCore.Http;

namespace backend.UnitTests.Hubs
{
    public class NotificationHubTests
    {
        private readonly Mock<ILogger<NotificationHub>> _mockLogger;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly NotificationHub _hub;
        private readonly string _userId = "1";

        public NotificationHubTests()
        {
            _mockLogger = new Mock<ILogger<NotificationHub>>();
            _mockContext = new Mock<HubCallerContext>();
            _mockGroups = new Mock<IGroupManager>();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, _userId) };
            var identity = new ClaimsIdentity(claims);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _mockContext.Setup(c => c.User).Returns(claimsPrincipal);
            _mockContext.Setup(c => c.ConnectionId).Returns("connection-id");

            _hub = new NotificationHub(_mockLogger.Object)
            {
                Context = _mockContext.Object,
                Groups = _mockGroups.Object
            };
        }

        [Fact]
        public async Task OnConnectedAsync_AuthenticatedUser_AddsToGroup()
        {
            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(
                groups => groups.AddToGroupAsync(
                    "connection-id",
                    _userId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_UnauthenticatedUser_ThrowsException()
        {
            // Arrange
            _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal)null!);

            // Act & Assert
            await Assert.ThrowsAsync<HubException>(
                () => _hub.OnConnectedAsync());
        }

        [Fact]
        public async Task OnDisconnectedAsync_AuthenticatedUser_RemovesFromGroup()
        {
            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(
                    "connection-id",
                    _userId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_UnauthenticatedUser_DoesNotThrow()
        {
            // Arrange
            _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal)null!);

            // Act & Assert
            await _hub.OnDisconnectedAsync(null);

            // Verify that RemoveFromGroupAsync was not called
            _mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
} 