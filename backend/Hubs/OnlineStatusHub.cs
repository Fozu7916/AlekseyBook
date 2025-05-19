using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using backend.Services;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Hubs.Interfaces;

namespace backend.Hubs
{
    [Authorize]
    public class OnlineStatusHub : Hub, IOnlineStatusHub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OnlineStatusHub> _logger;
        private static readonly Dictionary<string, HashSet<string>> _userConnections = new();
        private static readonly object _lock = new();

        public OnlineStatusHub(ApplicationDbContext context, ILogger<OnlineStatusHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new HubException("Пользователь не аутентифицирован");
                }

                lock (_lock)
                {
                    if (!_userConnections.ContainsKey(userId))
                    {
                        _userConnections[userId] = new HashSet<string>();
                    }
                    _userConnections[userId].Add(Context.ConnectionId);
                }

                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user != null && !user.IsOnline)
                {
                    user.IsOnline = true;
                    user.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await Clients.All.SendAsync("UserOnlineStatusChanged", new
                    {
                        UserId = user.Id,
                        IsOnline = true,
                        LastLogin = user.LastLogin
                    });
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подключении пользователя к OnlineStatusHub");
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    bool shouldUpdateStatus = false;

                    lock (_lock)
                    {
                        if (_userConnections.ContainsKey(userId))
                        {
                            _userConnections[userId].Remove(Context.ConnectionId);

                            if (_userConnections[userId].Count == 0)
                            {
                                _userConnections.Remove(userId);
                                shouldUpdateStatus = true;
                            }
                        }
                    }

                    if (shouldUpdateStatus)
                    {
                        var user = await _context.Users.FindAsync(int.Parse(userId));
                        if (user != null && user.IsOnline)
                        {
                            user.IsOnline = false;
                            user.LastLogin = DateTime.UtcNow;
                            await _context.SaveChangesAsync();

                            await Clients.All.SendAsync("UserOnlineStatusChanged", new
                            {
                                UserId = user.Id,
                                IsOnline = false,
                                LastLogin = user.LastLogin
                            });
                        }
                    }
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отключении пользователя от OnlineStatusHub");
                throw;
            }
        }

        public async Task GetOnlineUsers()
        {
            try
            {
                var onlineUsers = await _context.Users
                    .Where(u => u.IsOnline)
                    .Select(u => new
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        LastLogin = u.LastLogin
                    })
                    .ToListAsync();

                await Clients.Caller.SendAsync("OnlineUsersReceived", onlineUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка онлайн пользователей");
                throw;
            }
        }
    }
} 