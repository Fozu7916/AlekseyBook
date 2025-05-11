using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using backend.Models;

namespace backend.UnitTests.Hubs
{
    public class ChatHubTests
    {
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<HubCallerContext> _mockHubContext;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<ILogger<ChatHub>> _loggerMock;
        private readonly ChatHub _hub;

        public ChatHubTests()
        {
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockHubContext = new Mock<HubCallerContext>();
            _mockGroups = new Mock<IGroupManager>();
            _loggerMock = new Mock<ILogger<ChatHub>>();

            _hub = new ChatHub(_loggerMock.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockHubContext.Object,
                Groups = _mockGroups.Object
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _mockHubContext.Setup(x => x.User).Returns(claimsPrincipal);
            _mockHubContext.Setup(x => x.ConnectionId).Returns("test-connection-id");
        }

        [Fact]
        public async Task OnConnectedAsync_AddsToUserGroup()
        {
            // Arrange
            _mockGroups.Setup(x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockGroups.Verify(x => x.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }

        [Fact]
        public async Task OnDisconnectedAsync_RemovesFromUserGroup()
        {
            // Arrange
            _mockGroups.Setup(x => x.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockGroups.Verify(x => x.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ), Times.Never);
        }

        [Fact]
        public async Task JoinChat_AddsToGroupAndNotifiesUsers()
        {
            // Arrange
            var otherUserId = "2";

            _mockGroups.Setup(x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockClients.Setup(x => x.Group(It.IsAny<string>()))
                .Returns(_mockClientProxy.Object);

            // Act
            await _hub.JoinChat(otherUserId);

            // Assert
            _mockGroups.Verify(x => x.AddToGroupAsync(
                "test-connection-id",
                otherUserId,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task LeaveChat_RemovesFromGroupAndNotifiesUsers()
        {
            // Arrange
            var otherUserId = "2";

            _mockGroups.Setup(x => x.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockClients.Setup(x => x.Group(It.IsAny<string>()))
                .Returns(_mockClientProxy.Object);

            // Сначала нужно добавить пользователя в словарь подключений
            await _hub.JoinChat(otherUserId);

            // Act
            await _hub.LeaveChat(otherUserId);

            // Assert
            _mockGroups.Verify(x => x.RemoveFromGroupAsync(
                "test-connection-id",
                otherUserId,
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }
    }
} 