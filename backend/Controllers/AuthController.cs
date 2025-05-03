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
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IUserService userService, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginUserDto loginData)
        {
            try
            {
                _logger.LogInformation("Attempting login for email: {Email}", loginData.Email);
            
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginData.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", loginData.Email);
                    return BadRequest(new { message = "Неверный email или пароль" });
                }

                if (!_userService.VerifyPassword(loginData.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: Invalid password for user {Email}", loginData.Email);
                    return BadRequest(new { message = "Неверный email или пароль" });
                }

                _logger.LogInformation("User {Email} logged in successfully", loginData.Email);
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(user);

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
                _logger.LogError(ex, "Error during login for user {Email}", loginData.Email);
                return StatusCode(500, new { message = "Ошибка при входе в систему" });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserDto registerData)
        {
            try
            {
                _logger.LogInformation("Attempting registration for username: {Username}, email: {Email}", 
                    registerData.Username, registerData.Email);

                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerData.Email);
                if (existingUserByEmail != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", registerData.Email);
                    return BadRequest(new { message = "Этот email уже зарегистрирован" });
                }

                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerData.Username);
                if (existingUserByUsername != null)
                {
                    _logger.LogWarning("Registration failed: Username {Username} already exists", registerData.Username);
                    return BadRequest(new { message = "Это имя пользователя уже занято" });
                }

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

                _logger.LogInformation("User registered successfully: {Username} (ID: {UserId})", 
                    user.Username, user.Id);

                var token = GenerateJwtToken(user);

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
                _logger.LogError(ex, "Error during registration for user {Username}", registerData.Username);
                return StatusCode(500, new { message = "Ошибка при регистрации" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                _logger.LogError("JWT:Key not configured");
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