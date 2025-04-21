using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
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

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        [HttpGet("username/{username}")]
        public async Task<ActionResult<UserResponseDto>> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserResponseDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return await _userService.GetUsers(page, pageSize);
        }

        [HttpPost]
        public async Task<ActionResult<AuthResponseDto>> CreateUser([FromBody] RegisterUserDto registerUserDto)
        {
            try
            {
                Console.WriteLine($"Received registration request for username: {registerUserDto.Username}, email: {registerUserDto.Email}");
                var response = await _userService.Register(registerUserDto);
                Console.WriteLine("Registration successful");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userService.UpdateUser(id, updateUserDto);
                if (user == null)
                {
                    return NotFound();
                }
                return user;
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUser(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/avatar")]
        public async Task<ActionResult<UserResponseDto>> UpdateAvatar(int id, IFormFile avatar)
        {
            try
            {
                var user = await _userService.UpdateAvatar(id, avatar);
                if (user == null)
                {
                    return NotFound();
                }
                return user;
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
} 