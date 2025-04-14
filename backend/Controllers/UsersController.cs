using Backend.Models.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto createUserDto)
        {
            try
            {
                var user = await _userService.CreateUser(createUserDto);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return user;
        }

        [HttpGet("username/{username}")]
        public async Task<ActionResult<UserResponseDto>> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return user;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserResponseDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var users = await _userService.GetUsers(page, pageSize);
            return users;
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                if (updateUserDto == null)
                {
                    Console.WriteLine("UpdateUserDto is null");
                    return BadRequest(new { message = "Данные для обновления не предоставлены" });
                }

                if (string.IsNullOrEmpty(updateUserDto.Status))
                {
                    Console.WriteLine("Status is empty");
                    return BadRequest(new { message = "Статус не может быть пустым" });
                }

                Console.WriteLine($"Updating user {id} with data: Status='{updateUserDto.Status}', Bio='{updateUserDto.Bio}'");

                // Проверяем, что пользователь обновляет свой профиль
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"Current user ID from token: {userId}");

                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
                {
                    Console.WriteLine("Invalid user ID in token");
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                if (currentUserId != id)
                {
                    Console.WriteLine($"User {currentUserId} tried to update user {id}");
                    return Forbid();
                }

                var user = await _userService.UpdateUser(id, updateUserDto);
                if (user == null)
                {
                    Console.WriteLine($"User {id} not found");
                    return NotFound(new { message = "Пользователь не найден" });
                }

                Console.WriteLine($"User {id} updated successfully");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUser(id);
            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPost("avatar")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> UploadAvatar(IFormFile avatar)
        {
            try
            {
                if (avatar == null || avatar.Length == 0)
                {
                    return BadRequest(new { message = "Файл не выбран" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    return Unauthorized();
                }

                // Проверяем размер файла (максимум 5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "Размер файла не должен превышать 5MB" });
                }

                // Проверяем тип файла
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Допустимые форматы: JPG, JPEG, PNG, GIF" });
                }

                var user = await _userService.UpdateAvatar(id, avatar);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                return user;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка при загрузке аватара: {ex.Message}" });
            }
        }
    }
} 