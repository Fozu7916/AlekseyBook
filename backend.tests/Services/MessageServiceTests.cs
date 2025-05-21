using Xunit;
using Moq;
using backend.Services;
using backend.Services.Interfaces;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Collections;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;

namespace backend.UnitTests
{
    public class MessageServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock;
        private readonly MessageService _messageService;

        public MessageServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestMessageServiceDb")
                .Options;
            _context = new ApplicationDbContext(options);
            _userServiceMock = new Mock<IUserService>();
            _hubContextMock = new Mock<IHubContext<ChatHub>>();

            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(x => x.Groups(It.IsAny<IReadOnlyList<string>>()))
                .Returns(mockClientProxy.Object);
            _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);

            _messageService = new MessageService(_context, _userServiceMock.Object, _hubContextMock.Object);
        }

        [Fact]
        public async Task SendMessage_ValidData_ReturnsMessageDto()
        {
            // Arrange
            var sender = new User
            {
                Id = 1,
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active",
                AvatarUrl = "/uploads/avatar1.jpg"
            };

            var receiver = new User
            {
                Id = 2,
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active",
                AvatarUrl = "/uploads/avatar2.jpg"
            };

            var messageDto = new SendMessageDto
            {
                ReceiverId = receiver.Id,
                Content = "Test message"
            };

            var users = new List<User> { sender, receiver };
            var messages = new List<Message>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            using (var context = new TestApplicationDbContext(options))
            {
                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();

                var messageService = new MessageService(context, _userServiceMock.Object, _hubContextMock.Object);

                // Act
                var result = await messageService.SendMessage(sender.Id, messageDto);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(messageDto.Content, result.Content);
                Assert.Equal(sender.Id, result.Sender.Id);
                Assert.Equal(receiver.Id, result.Receiver.Id);
                Assert.Equal(sender.AvatarUrl, result.Sender.AvatarUrl);
                Assert.Equal(receiver.AvatarUrl, result.Receiver.AvatarUrl);
                Assert.False(result.IsRead);

                var savedMessage = await context.Messages.FirstOrDefaultAsync();
                Assert.NotNull(savedMessage);
                Assert.Equal(messageDto.Content, savedMessage.Content);
                Assert.Equal(sender.Id, savedMessage.SenderId);
                Assert.Equal(receiver.Id, savedMessage.ReceiverId);
                Assert.Equal(MessageStatus.Sent, savedMessage.Status);
            }
        }

        [Fact]
        public async Task SendMessage_InvalidSender_ThrowsException()
        {
            // Arrange
            var receiver = new User
            {
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active",
                AvatarUrl = "/uploads/avatar.jpg"
            };
            await _context.Users.AddAsync(receiver);
            await _context.SaveChangesAsync();

            var messageDto = new SendMessageDto
            {
                ReceiverId = receiver.Id,
                Content = "Test message"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _messageService.SendMessage(999, messageDto));
        }

        [Fact]
        public async Task GetChatMessages_ReturnsMessages()
        {
            // Arrange
            var sender = new User
            {
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(sender);

            var receiver = new User
            {
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver);

            var messages = new List<Message>
            {
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 1",
                    Status = MessageStatus.Read,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = receiver.Id,
                    Sender = receiver,
                    ReceiverId = sender.Id,
                    Receiver = sender,
                    Content = "Message 2",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var result = await _messageService.GetChatMessages(sender.Id, receiver.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.Content == "Message 1" && m.IsRead);
            Assert.Contains(result, m => m.Content == "Message 2" && !m.IsRead);
        }

        [Fact]
        public async Task MarkMessagesAsRead_MarksMessages()
        {
            // Arrange
            var sender = new User
            {
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(sender);

            var receiver = new User
            {
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver);

            var messages = new List<Message>
            {
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 1",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 2",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            await _messageService.MarkMessagesAsRead(receiver.Id, sender.Id);

            // Assert
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == sender.Id && m.ReceiverId == receiver.Id && m.Status == MessageStatus.Sent)
                .CountAsync();
            Assert.Equal(0, unreadMessages);

            var readMessages = await _context.Messages
                .Where(m => m.SenderId == sender.Id && m.ReceiverId == receiver.Id && m.Status == MessageStatus.Read)
                .CountAsync();
            Assert.Equal(2, readMessages);
        }

        [Fact]
        public async Task GetUnreadMessagesCount_ReturnsCorrectCount()
        {
            // Arrange
            var sender = new User
            {
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(sender);

            var receiver = new User
            {
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver);

            var messages = new List<Message>
            {
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 1",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 2",
                    Status = MessageStatus.Read,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 3",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var result = await _messageService.GetUnreadMessagesCount(receiver.Id);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task MarkMessagesAsRead_OnlyMarksTargetUserMessages()
        {
            // Arrange
            var sender = new User
            {
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(sender);

            var receiver1 = new User
            {
                Username = "receiver1",
                Email = "receiver1@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver1);

            var receiver2 = new User
            {
                Username = "receiver2",
                Email = "receiver2@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver2);

            var messages = new List<Message>
            {
                // Сообщения для receiver1
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver1.Id,
                    Receiver = receiver1,
                    Content = "Message 1",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                },
                // Сообщения для receiver2
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver2.Id,
                    Receiver = receiver2,
                    Content = "Message 2",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            await _messageService.MarkMessagesAsRead(receiver1.Id, sender.Id);

            // Assert
            // Проверяем, что сообщения для receiver1 помечены как прочитанные
            var receiver1Messages = await _context.Messages
                .Where(m => m.ReceiverId == receiver1.Id)
                .ToListAsync();
            Assert.All(receiver1Messages, m => Assert.Equal(MessageStatus.Read, m.Status));

            // Проверяем, что сообщения для receiver2 остались непрочитанными
            var receiver2Messages = await _context.Messages
                .Where(m => m.ReceiverId == receiver2.Id)
                .ToListAsync();
            Assert.All(receiver2Messages, m => Assert.Equal(MessageStatus.Sent, m.Status));
        }

        [Fact]
        public async Task GetChatMessages_CorrectlyMapsMessageStatus()
        {
            // Arrange
            var sender = new User
            {
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(sender);

            var receiver = new User
            {
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver);

            var messages = new List<Message>
            {
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Sent Message",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-2)
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Read Message",
                    Status = MessageStatus.Read,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var result = await _messageService.GetChatMessages(sender.Id, receiver.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            var sentMessage = result.First(m => m.Content == "Sent Message");
            var readMessage = result.First(m => m.Content == "Read Message");

            Assert.False(sentMessage.IsRead);
            Assert.True(readMessage.IsRead);
        }

        [Fact]
        public async Task GetUserChats_CorrectlyCountsUnreadMessages()
        {
            // Arrange
            var sender = new User
            {
                Username = "sender",
                Email = "sender@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(sender);

            var receiver = new User
            {
                Username = "receiver",
                Email = "receiver@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(receiver);

            var messages = new List<Message>
            {
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 1",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-3)
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 2",
                    Status = MessageStatus.Read,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-2)
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 3",
                    Status = MessageStatus.Sent,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var result = await _messageService.GetUserChats(receiver.Id);

            // Assert
            Assert.Single(result);
            var chat = result.First();
            Assert.Equal(2, chat.UnreadCount);
            Assert.Equal("Message 3", chat.LastMessage.Content);
            Assert.False(chat.LastMessage.IsRead);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }

    public class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
} 