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

namespace backend.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateUser(CreateUserDto createUserDto);
        Task<AuthResponseDto> Register(CreateUserDto createUserDto);
        Task<AuthResponseDto?> Login(LoginDto loginDto);
        Task<UserResponseDto?> GetUserById(int id);
        Task<UserResponseDto?> GetUserByUsername(string username);
        Task<List<UserResponseDto>> GetUsers(int page = 1, int pageSize = 10);
        Task<UserResponseDto?> UpdateUser(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUser(int id);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        Task<UserResponseDto?> UpdateAvatar(int userId, IFormFile avatar);
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public UserService(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<UserResponseDto> CreateUser(CreateUserDto createUserDto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == createUserDto.Username || u.Email == createUserDto.Email);

            if (existingUser != null)
            {
                throw new Exception("Username or email already exists");
            }

            var user = new User
            {
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                PasswordHash = HashPassword(createUserDto.Password),
                AvatarUrl = createUserDto.AvatarUrl,
                Bio = createUserDto.Bio
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<AuthResponseDto> Register(CreateUserDto createUserDto)
        {
            var user = await CreateUser(createUserDto);
            var token = GenerateJwtToken(user);
            
            return new AuthResponseDto
            {
                Token = token,
                User = user
            };
        }

        public async Task<AuthResponseDto?> Login(LoginDto loginDto)
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
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
            try 
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return null;

                if (updateUserDto.Status.Length > 50)
                {
                    throw new Exception("Статус не может быть длиннее 50 символов");
                }

                if (!string.IsNullOrEmpty(updateUserDto.Status))
                {
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
                Console.Error.WriteLine($"Error updating user: {ex}");
                throw new Exception("Ошибка при обновлении пользователя", ex);
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
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<UserResponseDto?> UpdateAvatar(int userId, IFormFile avatar)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "avatars");
            
            Console.WriteLine($"Uploading avatar to: {uploadsFolder}");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                Console.WriteLine($"Created directory: {uploadsFolder}");
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            Console.WriteLine($"Saving file to: {filePath}");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }
            Console.WriteLine($"File saved successfully: {filePath}");

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldFilePath = Path.Combine(_environment.ContentRootPath, "wwwroot", user.AvatarUrl.TrimStart('/'));
                Console.WriteLine($"Checking old file: {oldFilePath}");
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                    Console.WriteLine($"Old file deleted: {oldFilePath}");
                }
            }

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            Console.WriteLine($"Avatar URL updated in database: {user.AvatarUrl}");
            
            return MapToDto(user);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        private static UserResponseDto MapToDto(User user)
        {
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
    }
} 