using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public MessageService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<MessageDto> SendMessage(int senderId, SendMessageDto messageDto)
        {
            var sender = await _context.Users.FindAsync(senderId)
                ?? throw new Exception("Отправитель не найден");
            
            var receiver = await _context.Users.FindAsync(messageDto.ReceiverId)
                ?? throw new Exception("Получатель не найден");

            var message = new Message
            {
                SenderId = senderId,
                Sender = sender,
                ReceiverId = messageDto.ReceiverId,
                Receiver = receiver,
                Content = messageDto.Content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt,
                Sender = new UserResponseDto
                {
                    Id = sender.Id,
                    Username = sender.Username,
                    Email = sender.Email,
                    AvatarUrl = sender.AvatarUrl,
                    Status = sender.Status,
                    CreatedAt = sender.CreatedAt,
                    LastLogin = sender.LastLogin,
                    IsVerified = sender.IsVerified,
                    Bio = sender.Bio
                },
                Receiver = new UserResponseDto
                {
                    Id = receiver.Id,
                    Username = receiver.Username,
                    Email = receiver.Email,
                    AvatarUrl = receiver.AvatarUrl,
                    Status = receiver.Status,
                    CreatedAt = receiver.CreatedAt,
                    LastLogin = receiver.LastLogin,
                    IsVerified = receiver.IsVerified,
                    Bio = receiver.Bio
                }
            };
        }

        public async Task<List<MessageDto>> GetChatMessages(int userId, int otherUserId)
        {
            var messages = await _context.Messages
                .Where(m => 
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            var userIds = messages
                .SelectMany(m => new[] { m.SenderId, m.ReceiverId })
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    IsVerified = u.IsVerified,
                    Bio = u.Bio
                })
                .ToDictionaryAsync(u => u.Id);

            return messages
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt,
                    Sender = users.GetValueOrDefault(m.SenderId) ?? throw new Exception($"Sender not found: {m.SenderId}"),
                    Receiver = users.GetValueOrDefault(m.ReceiverId) ?? throw new Exception($"Receiver not found: {m.ReceiverId}")
                })
                .ToList();
        }

        public async Task<List<ChatPreviewDto>> GetUserChats(int userId)
        {
            var query = from m in _context.Messages
                        where m.SenderId == userId || m.ReceiverId == userId
                        group m by m.SenderId == userId ? m.ReceiverId : m.SenderId into g
                        select new
                        {
                            OtherUserId = g.Key,
                            LastMessage = g.OrderByDescending(x => x.CreatedAt).First(),
                            UnreadCount = g.Count(x => !x.IsRead && x.ReceiverId == userId)
                        };

            var results = await query.ToListAsync();
            
            var otherUserIds = results.Select(x => x.OtherUserId).ToList();
            var users = await _context.Users
                .Where(u => otherUserIds.Contains(u.Id))
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl,
                    Status = u.Status,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin,
                    IsVerified = u.IsVerified,
                    Bio = u.Bio
                })
                .ToDictionaryAsync(u => u.Id);

            return results
                .Where(chat => 
                    users.ContainsKey(chat.OtherUserId) && 
                    users.ContainsKey(chat.LastMessage.SenderId) && 
                    users.ContainsKey(chat.LastMessage.ReceiverId))
                .Select(chat => new ChatPreviewDto
                {
                    User = users[chat.OtherUserId],
                    LastMessage = new MessageDto
                    {
                        Id = chat.LastMessage.Id,
                        Content = chat.LastMessage.Content,
                        IsRead = chat.LastMessage.IsRead,
                        CreatedAt = chat.LastMessage.CreatedAt,
                        Sender = users[chat.LastMessage.SenderId],
                        Receiver = users[chat.LastMessage.ReceiverId]
                    },
                    UnreadCount = chat.UnreadCount
                })
                .OrderByDescending(c => c.LastMessage.CreatedAt)
                .ToList();
        }

        public async Task MarkMessagesAsRead(int userId, int otherUserId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => 
                    m.SenderId == otherUserId && 
                    m.ReceiverId == userId && 
                    !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadMessagesCount(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        }

        private MessageDto MapToMessageDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                Sender = new UserResponseDto
                {
                    Id = message.Sender.Id,
                    Username = message.Sender.Username,
                    Email = message.Sender.Email,
                    AvatarUrl = message.Sender.AvatarUrl,
                    Status = message.Sender.Status,
                    CreatedAt = message.Sender.CreatedAt,
                    LastLogin = message.Sender.LastLogin,
                    IsVerified = message.Sender.IsVerified,
                    Bio = message.Sender.Bio
                },
                Receiver = new UserResponseDto
                {
                    Id = message.Receiver.Id,
                    Username = message.Receiver.Username,
                    Email = message.Receiver.Email,
                    AvatarUrl = message.Receiver.AvatarUrl,
                    Status = message.Receiver.Status,
                    CreatedAt = message.Receiver.CreatedAt,
                    LastLogin = message.Receiver.LastLogin,
                    IsVerified = message.Receiver.IsVerified,
                    Bio = message.Receiver.Bio
                },
                Content = message.Content,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };
        }
    }
} 