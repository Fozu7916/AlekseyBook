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

namespace backend.UnitTests
{
    public class MessageServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly MessageService _messageService;

        public MessageServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestMessageServiceDb")
                .Options;
            _context = new ApplicationDbContext(options);
            _userServiceMock = new Mock<IUserService>();
            _messageService = new MessageService(_context, _userServiceMock.Object);
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

                var messageService = new MessageService(context, _userServiceMock.Object);

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
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = receiver.Id,
                    Sender = receiver,
                    ReceiverId = sender.Id,
                    Receiver = sender,
                    Content = "Message 2",
                    IsRead = false,
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
            Assert.Contains(result, m => m.Content == "Message 1");
            Assert.Contains(result, m => m.Content == "Message 2");
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
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 2",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            };
            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            await _messageService.MarkMessagesAsRead(receiver.Id, sender.Id);

            // Assert
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == sender.Id && m.ReceiverId == receiver.Id && !m.IsRead)
                .CountAsync();
            Assert.Equal(0, unreadMessages);
        }

        [Fact]
        public async Task GetUnreadMessagesCount_ReturnsCount()
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
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 2",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Message
                {
                    SenderId = sender.Id,
                    Sender = sender,
                    ReceiverId = receiver.Id,
                    Receiver = receiver,
                    Content = "Message 3",
                    IsRead = true,
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