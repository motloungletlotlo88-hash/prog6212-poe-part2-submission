using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using System.Security.Claims;

namespace RaceDay.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public UsersController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>
        /// Get the logged-in user's own profile details.
        /// </summary>
        /// <returns>The authenticated user's profile information.</returns>
        /// <response code="200">Returns the user's profile.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the user is not found.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == GetUserId());
            if (user == null) return NotFound();
            return Ok(new { user.UserID, user.FullName, user.Email, user.PhoneNumber, role = user.Role?.RoleName, user.CreatedAt });
        }

        /// <summary>
        /// Update the logged-in user's profile information.
        /// </summary>
        /// <param name="dto">The updated FullName and PhoneNumber.</param>
        /// <returns>The updated user profile.</returns>
        /// <response code="200">Returns the updated user profile.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpPut("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            await _context.SaveChangesAsync();
            return Ok(new { user.UserID, user.FullName, user.Email, user.PhoneNumber });
        }

        /// <summary>
        /// Change the logged-in user's password.
        /// </summary>
        /// <param name="dto">The CurrentPassword and NewPassword.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">If the password was updated.</response>
        /// <response code="400">If the current password is incorrect.</response>
        [HttpPut("me/password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] UpdatePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash)) return BadRequest("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully." });
        }

        /// <summary>
        /// View a limited public profile of a user.
        /// </summary>
        /// <param name="id">The ID of the user to view.</param>
        /// <returns>The user's public profile (ID, Name, Role).</returns>
        /// <response code="200">Returns the public profile.</response>
        /// <response code="404">If the user is not found.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublicProfile(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null) return NotFound("User not found.");
            return Ok(new { user.UserID, user.FullName, role = user.Role?.RoleName });
        }
    }
}