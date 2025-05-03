using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Models.DTOs;
using backend.Services;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace backend.UnitTests
{
    public class MessagesControllerTests
    {
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock;
        private readonly Mock<ILogger<MessagesController>> _loggerMock;
        private readonly MessagesController _controller;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly Mock<IHubClients> _hubClientsMock;

        public MessagesControllerTests()
        {
            _messageServiceMock = new Mock<IMessageService>();
            _hubContextMock = new Mock<IHubContext<ChatHub>>();
            _loggerMock = new Mock<ILogger<MessagesController>>();
            _clientProxyMock = new Mock<IClientProxy>();
            _hubClientsMock = new Mock<IHubClients>();

            _hubClientsMock.Setup(x => x.Group(It.IsAny<string>()))
                .Returns(_clientProxyMock.Object);
            _hubContextMock.Setup(x => x.Clients)
                .Returns(_hubClientsMock.Object);

            _controller = new MessagesController(_messageServiceMock.Object, _hubContextMock.Object, _loggerMock.Object);

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
        public async Task SendMessage_ValidMessage_ReturnsOkResult()
        {
            // Arrange
            var messageDto = new SendMessageDto
            {
                ReceiverId = 2,
                Content = "Test message"
            };

            var messageResponse = new MessageDto
            {
                Id = 1,
                Content = messageDto.Content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Sender = new UserResponseDto 
                { 
                    Id = 1, 
                    Username = "testuser",
                    Email = "test@example.com",
                    Status = "Active"
                },
                Receiver = new UserResponseDto 
                { 
                    Id = 2, 
                    Username = "receiver",
                    Email = "receiver@example.com",
                    Status = "Active"
                }
            };

            _messageServiceMock.Setup(x => x.SendMessage(1, messageDto))
                .ReturnsAsync(messageResponse);

            _clientProxyMock.Setup(x => x.SendCoreAsync(
                "ReceiveMessage",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SendMessage(messageDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<MessageDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnValue = Assert.IsType<MessageDto>(okResult.Value);
            Assert.Equal(messageDto.Content, returnValue.Content);
            Assert.Equal(1, returnValue.Sender.Id);
            Assert.Equal(2, returnValue.Receiver.Id);

            _clientProxyMock.Verify(x => x.SendCoreAsync(
                "ReceiveMessage",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task SendMessage_InvalidMessage_ReturnsBadRequest()
        {
            // Arrange
            var messageDto = new SendMessageDto
            {
                ReceiverId = 2,
                Content = "Test message"
            };

            _messageServiceMock.Setup(x => x.SendMessage(1, messageDto))
                .ThrowsAsync(new Exception("Ошибка при отправке сообщения"));

            // Act
            var result = await _controller.SendMessage(messageDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<MessageDto>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при отправке сообщения", errorMessage);
        }

        [Fact]
        public async Task GetChatMessages_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var messages = new List<MessageDto>
            {
                new MessageDto
                {
                    Id = 1,
                    Content = "Test message 1",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow,
                    Sender = new UserResponseDto 
                    { 
                        Id = 1, 
                        Username = "testuser",
                        Email = "test@example.com",
                        Status = "Active"
                    },
                    Receiver = new UserResponseDto 
                    { 
                        Id = 2, 
                        Username = "receiver",
                        Email = "receiver@example.com",
                        Status = "Active"
                    }
                },
                new MessageDto
                {
                    Id = 2,
                    Content = "Test message 2",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Sender = new UserResponseDto 
                    { 
                        Id = 2, 
                        Username = "receiver",
                        Email = "receiver@example.com",
                        Status = "Active"
                    },
                    Receiver = new UserResponseDto 
                    { 
                        Id = 1, 
                        Username = "testuser",
                        Email = "test@example.com",
                        Status = "Active"
                    }
                }
            };

            _messageServiceMock.Setup(x => x.GetChatMessages(1, 2))
                .ReturnsAsync(messages);

            // Act
            var result = await _controller.GetChatMessages(2);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<MessageDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnValue = Assert.IsType<List<MessageDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal("Test message 1", returnValue[0].Content);
            Assert.Equal("Test message 2", returnValue[1].Content);
        }

        [Fact]
        public async Task GetChatMessages_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            _messageServiceMock.Setup(x => x.GetChatMessages(1, 2))
                .ThrowsAsync(new Exception("Ошибка при получении сообщений"));

            // Act
            var result = await _controller.GetChatMessages(2);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<MessageDto>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при получении сообщений", errorMessage);
        }

        [Fact]
        public async Task GetUserChats_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var chats = new List<ChatPreviewDto>
            {
                new ChatPreviewDto
                {
                    User = new UserResponseDto 
                    { 
                        Id = 2, 
                        Username = "user2",
                        Email = "user2@example.com",
                        Status = "Active"
                    },
                    LastMessage = new MessageDto
                    {
                        Id = 1,
                        Content = "Last message 1",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Sender = new UserResponseDto 
                        { 
                            Id = 2, 
                            Username = "user2",
                            Email = "user2@example.com",
                            Status = "Active"
                        },
                        Receiver = new UserResponseDto 
                        { 
                            Id = 1, 
                            Username = "testuser",
                            Email = "test@example.com",
                            Status = "Active"
                        }
                    },
                    UnreadCount = 1
                },
                new ChatPreviewDto
                {
                    User = new UserResponseDto 
                    { 
                        Id = 3, 
                        Username = "user3",
                        Email = "user3@example.com",
                        Status = "Active"
                    },
                    LastMessage = new MessageDto
                    {
                        Id = 2,
                        Content = "Last message 2",
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow,
                        Sender = new UserResponseDto 
                        { 
                            Id = 1, 
                            Username = "testuser",
                            Email = "test@example.com",
                            Status = "Active"
                        },
                        Receiver = new UserResponseDto 
                        { 
                            Id = 3, 
                            Username = "user3",
                            Email = "user3@example.com",
                            Status = "Active"
                        }
                    },
                    UnreadCount = 0
                }
            };

            _messageServiceMock.Setup(x => x.GetUserChats(1))
                .ReturnsAsync(chats);

            // Act
            var result = await _controller.GetUserChats();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<ChatPreviewDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnValue = Assert.IsType<List<ChatPreviewDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal("user2", returnValue[0].User.Username);
            Assert.Equal("user3", returnValue[1].User.Username);
        }

        [Fact]
        public async Task GetUserChats_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            _messageServiceMock.Setup(x => x.GetUserChats(1))
                .ThrowsAsync(new Exception("Ошибка при получении чатов"));

            // Act
            var result = await _controller.GetUserChats();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<ChatPreviewDto>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при получении чатов", errorMessage);
        }

        [Fact]
        public async Task MarkMessagesAsRead_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            _messageServiceMock.Setup(x => x.MarkMessagesAsRead(1, 2))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkMessagesAsRead(2);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task MarkMessagesAsRead_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            _messageServiceMock.Setup(x => x.MarkMessagesAsRead(1, 2))
                .ThrowsAsync(new Exception("Ошибка при отметке сообщений как прочитанных"));

            // Act
            var result = await _controller.MarkMessagesAsRead(2);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при отметке сообщений как прочитанных", errorMessage);
        }

        [Fact]
        public async Task GetUnreadMessagesCount_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            _messageServiceMock.Setup(x => x.GetUnreadMessagesCount(1))
                .ReturnsAsync(5);

            // Act
            var result = await _controller.GetUnreadMessagesCount();

            // Assert
            var actionResult = Assert.IsType<ActionResult<int>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var count = Assert.IsType<int>(okResult.Value);
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task GetUnreadMessagesCount_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            _messageServiceMock.Setup(x => x.GetUnreadMessagesCount(1))
                .ThrowsAsync(new Exception("Ошибка при получении количества непрочитанных сообщений"));

            // Act
            var result = await _controller.GetUnreadMessagesCount();

            // Assert
            var actionResult = Assert.IsType<ActionResult<int>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Ошибка при получении количества непрочитанных сообщений", errorMessage);
        }
    }
} 