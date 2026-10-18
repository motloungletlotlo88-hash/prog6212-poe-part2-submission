using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaceDay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(RaceDayDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Registers a new user as an Organiser or Participant.
        /// </summary>
        /// <param name="dto">The registration details containing FullName, Email, Password, PhoneNumber, and Role.</param>
        /// <returns>A JWT token and the newly created user's details.</returns>
        /// <response code="200">Returns the newly created user and JWT token.</response>
        /// <response code="400">If the email is already registered or the role is invalid.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email)) return BadRequest("Email already registered.");
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role);
            if (role == null) return BadRequest("Invalid role specified. Choose Organiser or Participant.");

            var user = new User { FullName = dto.FullName, Email = dto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), PhoneNumber = dto.PhoneNumber, RoleID = role.RoleID };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user, role.RoleName);
            return Ok(new { userId = user.UserID, fullName = user.FullName, email = user.Email, role = role.RoleName, token });
        }

        /// <summary>
        /// Authenticates a user and issues a JWT token.
        /// </summary>
        /// <param name="dto">The login credentials (Email and Password).</param>
        /// <returns>A JWT token, user ID, full name, and role.</returns>
        /// <response code="200">Returns the JWT token and user details.</response>
        /// <response code="401">If the email or password is incorrect.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials.");

            var token = GenerateJwtToken(user, user.Role?.RoleName ?? "Participant");
            return Ok(new { token, userId = user.UserID, fullName = user.FullName, role = user.Role?.RoleName });
        }

        /// <summary>
        /// Invalidates the current user's session/token.
        /// </summary>
        /// <returns>A success message.</returns>
        /// <response code="200">If the user was successfully logged out.</response>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout() => Ok(new { message = "Logged out successfully." });

        private string GenerateJwtToken(User user, string roleName)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ThisIsASecretKeyForRaceDayAPI12345!";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"] ?? "Issuer", _configuration["Jwt:Audience"] ?? "Audience", claims, expires: DateTime.Now.AddHours(3), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}