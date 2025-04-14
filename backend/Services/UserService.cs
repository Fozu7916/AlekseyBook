using System.Security.Cryptography;
using System.Text;
using Backend.Data;
using Backend.Models;
using Backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;

namespace Backend.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateUser(CreateUserDto createUserDto);
        Task<UserResponseDto?> GetUserById(int id);
        Task<UserResponseDto?> GetUserByUsername(string username);
        Task<List<UserResponseDto>> GetUsers(int page = 1, int pageSize = 10);
        Task<UserResponseDto?> UpdateUser(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUser(int id);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        Task<UserResponseDto?> UpdateAvatar(int userId, IFormFile avatar);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(updateUserDto.Username) && updateUserDto.Username != user.Username)
            {
                var existingUsername = await _context.Users.AnyAsync(u => u.Username == updateUserDto.Username);
                if (existingUsername)
                {
                    throw new Exception("Username already exists");
                }
                user.Username = updateUserDto.Username;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                var existingEmail = await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email);
                if (existingEmail)
                {
                    throw new Exception("Email already exists");
                }
                user.Email = updateUserDto.Email;
            }

            if (!string.IsNullOrEmpty(updateUserDto.AvatarUrl))
                user.AvatarUrl = updateUserDto.AvatarUrl;

            if (!string.IsNullOrEmpty(updateUserDto.Bio))
                user.Bio = updateUserDto.Bio;

            if (!string.IsNullOrEmpty(updateUserDto.Status))
                user.Status = updateUserDto.Status;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(user);
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

            // Создаем уникальное имя файла
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "avatars");
            
            Console.WriteLine($"Uploading avatar to: {uploadsFolder}");
            
            // Создаем директорию, если она не существует
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                Console.WriteLine($"Created directory: {uploadsFolder}");
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            Console.WriteLine($"Saving file to: {filePath}");

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }
            Console.WriteLine($"File saved successfully: {filePath}");

            // Удаляем старый аватар, если он существует
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

            // Обновляем URL аватара в базе данных
            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            Console.WriteLine($"Avatar URL updated in database: {user.AvatarUrl}");
            
            return MapToDto(user);
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