using System.Security.Cryptography;
using System.Text;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using backend.Services.Interfaces;

namespace backend.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration, ILogger<UserService> logger)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> Register(RegisterUserDto registerUserDto)
        {
            try
            {
                _logger.LogInformation("Attempting registration for username: {Username}, email: {Email}",
                    registerUserDto.Username, registerUserDto.Email);

                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerUserDto.Email);
                if (existingUserByEmail != null)
                {
                    throw new Exception("Этот email уже зарегистрирован");
                }

                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerUserDto.Username);
                if (existingUserByUsername != null)
                {
                    throw new Exception("Это имя пользователя уже занято");
                }

                var user = new User
                {
                    Username = registerUserDto.Username,
                    Email = registerUserDto.Email,
                    PasswordHash = HashPassword(registerUserDto.Password),
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    Status = "Новый пользователь",
                    IsVerified = false,
                    IsOnline = false,
                    IsBanned = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

                var userDto = MapToDto(user);
                var token = GenerateJwtToken(userDto);

                return new AuthResponseDto
                {
                    Token = token,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for username: {Username}", registerUserDto.Username);
                throw;
            }
        }

        public async Task<AuthResponseDto?> Login(LoginUserDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var userDto = MapToDto(user);
            var token = GenerateJwtToken(userDto);
            
            return new AuthResponseDto
            {
                Token = token,
                User = userDto
            };
        }

        private string GenerateJwtToken(UserResponseDto user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<UserResponseDto?> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<UserResponseDto?> GetUserByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user != null ? MapToDto(user) : null;
        }

        public async Task<List<UserResponseDto>> GetUsers(int page = 1, int pageSize = 10)
        {
            return await _context.Users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => MapToDto(u))
                .ToListAsync();
        }

        public async Task<UserResponseDto?> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            if (updateUserDto == null)
                throw new ArgumentNullException(nameof(updateUserDto));

            try 
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return null;

                if (!string.IsNullOrEmpty(updateUserDto.Status))
                {
                    if (updateUserDto.Status.Length > 50)
                    {
                        throw new Exception("Статус не может быть длиннее 50 символов");
                    }
                    user.Status = updateUserDto.Status;
                }

                if (updateUserDto.Bio != null)
                {
                    if (updateUserDto.Bio.Length > 1000)
                    {
                        throw new Exception("Биография не может быть длиннее 1000 символов");
                    }
                    user.Bio = updateUserDto.Bio;
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return MapToDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(hashedPassword))
                throw new ArgumentNullException(nameof(hashedPassword));

            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<UserResponseDto?> UpdateAvatar(int userId, IFormFile avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (avatar.Length > 10 * 1024 * 1024) // 10MB
            {
                throw new Exception("Размер файла превышает 10MB");
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(avatar.ContentType.ToLower()))
            {
                throw new Exception("Недопустимый формат файла. Разрешены только JPEG, PNG и GIF");
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "avatars");
            
            _logger.LogInformation("Uploading avatar for user {UserId} to {UploadPath}", userId, uploadsFolder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation("Created avatars directory: {UploadPath}", uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            _logger.LogDebug("Saving avatar file to: {FilePath}", filePath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldFilePath = Path.Combine(_environment.ContentRootPath, "wwwroot", user.AvatarUrl.TrimStart('/'));
                _logger.LogDebug("Checking old avatar file: {OldFilePath}", oldFilePath);
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                    _logger.LogInformation("Deleted old avatar file: {OldFilePath}", oldFilePath);
                }
            }

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated avatar URL in database for user {UserId}: {AvatarUrl}", 
                userId, user.AvatarUrl);
            
            return MapToDto(user);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new Exception($"User not found: {id}");
            return user;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                throw new Exception($"User not found: {username}");
            return user;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception($"User not found: {email}");
            return user;
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        private static UserResponseDto MapToDto(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsVerified = user.IsVerified,
                Bio = user.Bio
            };
        }

        public async Task<(UserResponseDto User, string Token)> RegisterUserAsync(RegisterUserDto registerData)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerData.Email))
            {
                throw new Exception("Пользователь с таким email уже существует");
            }

            if (await _context.Users.AnyAsync(u => u.Username == registerData.Username))
            {
                throw new Exception("Пользователь с таким именем уже существует");
            }

            var user = new User
            {
                Username = registerData.Username,
                Email = registerData.Email,
                PasswordHash = HashPassword(registerData.Password),
                Status = "online",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                IsVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username ?? throw new Exception("Username не может быть null"),
                Email = user.Email ?? throw new Exception("Email не может быть null"),
                Status = user.Status,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsVerified = user.IsVerified,
                Bio = user.Bio
            };

            return (response, GenerateJwtToken(response));
        }

        public async Task<(UserResponseDto User, string Token)> LoginUserAsync(LoginUserDto loginData)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginData.Email);
            
            if (user == null || !VerifyPassword(loginData.Password, user.PasswordHash))
            {
                throw new Exception("Неверный email или пароль");
            }

            var userDto = MapToDto(user);
            var token = GenerateJwtToken(userDto);
            
            return (userDto, token);
        }

        public async Task<string> UpdateUserAvatarAsync(int id, string avatarUrl)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) throw new Exception("Пользователь не найден");

            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return avatarUrl;
        }

        public async Task<List<UserResponseDto>> SearchUsers(string searchTerm, int page = 1, int pageSize = 10)
        {
            return await _context.Users
                .Where(u => u.Username.Contains(searchTerm))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => MapToDto(u))
                .ToListAsync();
        }
    }
} 