using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessage(int senderId, SendMessageDto messageDto);
        Task<List<MessageDto>> GetChatMessages(int userId, int otherUserId);
        Task<List<ChatPreviewDto>> GetUserChats(int userId);
        Task MarkMessagesAsRead(int userId, int otherUserId);
        Task<int> GetUnreadMessagesCount(int userId);
    }

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
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = messageDto.ReceiverId,
                Content = messageDto.Content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return await MapToMessageDto(message);
        }

        public async Task<List<MessageDto>> GetChatMessages(int userId, int otherUserId)
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => 
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            var messageDtos = new List<MessageDto>();
            foreach (var message in messages)
            {
                messageDtos.Add(await MapToMessageDto(message));
            }
            return messageDtos;
        }

        public async Task<List<ChatPreviewDto>> GetUserChats(int userId)
        {
            var lastMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).First(),
                    UnreadCount = g.Count(m => !m.IsRead && m.ReceiverId == userId)
                })
                .ToListAsync();

            var chatPreviews = new List<ChatPreviewDto>();
            foreach (var chat in lastMessages)
            {
                var otherUser = await _userService.GetUserByIdAsync(chat.OtherUserId);
                chatPreviews.Add(new ChatPreviewDto
                {
                    User = new UserResponseDto
                    {
                        Id = otherUser.Id,
                        Username = otherUser.Username,
                        Email = otherUser.Email,
                        AvatarUrl = otherUser.AvatarUrl,
                        Status = otherUser.Status,
                        CreatedAt = otherUser.CreatedAt,
                        LastLogin = otherUser.LastLogin,
                        IsVerified = otherUser.IsVerified,
                        Bio = otherUser.Bio
                    },
                    LastMessage = await MapToMessageDto(chat.LastMessage),
                    UnreadCount = chat.UnreadCount
                });
            }

            return chatPreviews.OrderByDescending(c => c.LastMessage.CreatedAt).ToList();
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

        private async Task<MessageDto> MapToMessageDto(Message message)
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