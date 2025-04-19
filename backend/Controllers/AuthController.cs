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
        public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginDto loginData)
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