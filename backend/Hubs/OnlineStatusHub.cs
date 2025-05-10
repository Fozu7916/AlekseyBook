using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using backend.Services;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Hubs
{
    [Authorize]
    public class OnlineStatusHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OnlineStatusHub> _logger;
        private static readonly Dictionary<string, UserConnectionInfo> _userConnections = new();

        private class UserConnectionInfo
        {
            public Dictionary<string, bool> ConnectionStates { get; set; } = new();

            public bool IsAnyConnectionFocused => ConnectionStates.Values.Any(focused => focused);
        }

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
                if (userId == null)
                {
                    throw new HubException("Пользователь не аутентифицирован");
                }

                if (!_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId] = new UserConnectionInfo 
                    { 
                        ConnectionStates = new Dictionary<string, bool> { { Context.ConnectionId, true } }
                    };
                }
                else
                {
                    _userConnections[userId].ConnectionStates[Context.ConnectionId] = true;
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
                if (userId != null && _userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].ConnectionStates.Remove(Context.ConnectionId);

                    // Если это было последнее соединение пользователя
                    if (_userConnections[userId].ConnectionStates.Count == 0)
                    {
                        _userConnections.Remove(userId);

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
                    else
                    {
                        var isAnyConnectionFocused = _userConnections[userId].IsAnyConnectionFocused;
                        var user = await _context.Users.FindAsync(int.Parse(userId));
                        if (user != null && user.IsOnline != isAnyConnectionFocused)
                        {
                            user.IsOnline = isAnyConnectionFocused;
                            if (!isAnyConnectionFocused)
                            {
                                user.LastLogin = DateTime.UtcNow;
                            }
                            await _context.SaveChangesAsync();

                            await Clients.All.SendAsync("UserOnlineStatusChanged", new
                            {
                                UserId = user.Id,
                                IsOnline = isAnyConnectionFocused,
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

        public async Task UpdateFocusState(bool isFocused)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    throw new HubException("Пользователь не аутентифицирован");
                }

                if (_userConnections.ContainsKey(userId))
                {
                    var connectionInfo = _userConnections[userId];
                    connectionInfo.ConnectionStates[Context.ConnectionId] = isFocused;
                    
                    var isAnyConnectionFocused = connectionInfo.IsAnyConnectionFocused;
                    var user = await _context.Users.FindAsync(int.Parse(userId));
                    
                    if (user != null)
                    {
                        var wasOnline = user.IsOnline;
                        user.IsOnline = isAnyConnectionFocused;

                        if (wasOnline && !isAnyConnectionFocused)
                        {
                            user.LastLogin = DateTime.UtcNow;
                        }
                        
                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "Обновление статуса пользователя {UserId}: IsOnline={IsOnline}, WasOnline={WasOnline}, IsFocused={IsFocused}, IsAnyConnectionFocused={IsAnyConnectionFocused}",
                            userId, user.IsOnline, wasOnline, isFocused, isAnyConnectionFocused);

                        await Clients.All.SendAsync("UserOnlineStatusChanged", new
                        {
                            UserId = user.Id,
                            IsOnline = isAnyConnectionFocused,
                            LastLogin = user.LastLogin
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении состояния фокуса пользователя");
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