using Microsoft.AspNetCore.Mvc;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginUserDto loginData)
        {
            try
            {
                Console.WriteLine($"Attempting login for email: {loginData.Email}");
            
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginData.Email);

                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return BadRequest(new { message = "Неверный email или пароль" });
                }

                Console.WriteLine("User found, verifying password");
                if (!_userService.VerifyPassword(loginData.Password, user.PasswordHash))
                {
                    Console.WriteLine("Password verification failed");
                    return BadRequest(new { message = "Неверный email или пароль" });
                }

                Console.WriteLine("Password verified successfully");
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                Console.WriteLine("JWT token generated successfully");

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    User = new UserResponseDto
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
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Ошибка при входе в систему: {ex.Message}" });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserDto registerData)
        {
            try
            {
                Console.WriteLine($"Attempting registration for username: {registerData.Username}, email: {registerData.Email}");

                // Проверяем, не занят ли email
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerData.Email);
                if (existingUserByEmail != null)
                {
                    return BadRequest(new { message = "Этот email уже зарегистрирован" });
                }

                // Проверяем, не занято ли имя пользователя
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerData.Username);
                if (existingUserByUsername != null)
                {
                    return BadRequest(new { message = "Это имя пользователя уже занято" });
                }

                // Создаем нового пользователя
                var user = new User
                {
                    Username = registerData.Username,
                    Email = registerData.Email,
                    PasswordHash = _userService.HashPassword(registerData.Password),
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    Status = "Новый пользователь",
                    IsVerified = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"User created successfully with ID: {user.Id}");

                var token = GenerateJwtToken(user);
                Console.WriteLine("JWT token generated successfully");

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    User = new UserResponseDto
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
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Ошибка при регистрации: {ex.Message}" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT:Key не настроен в конфигурации");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 