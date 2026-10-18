using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;
using System.Security.Claims;

namespace RaceDay.Controllers
{
    [Route("api")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EnrolmentsController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>
        /// View all available categories for a specific event.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>A list of categories.</returns>
        /// <response code="200">Returns the list of categories.</response>
        [HttpGet("events/{eventId}/categories")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(int eventId) => Ok(await _context.Categories.Where(c => c.EventID == eventId).ToListAsync());

        /// <summary>
        /// Enroll the logged-in participant into a specific category.
        /// </summary>
        /// <param name="categoryId">The ID of the category to enroll in.</param>
        /// <returns>The enrolment details.</returns>
        /// <response code="200">Returns the enrolment confirmation.</response>
        /// <response code="400">If already enrolled or the category is full.</response>
        /// <response code="403">If the user is not a Participant.</response>
        [HttpPost("categories/{categoryId}/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Enrol(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null) return NotFound("Category not found.");

            var participantId = GetUserId();
            if (await _context.Enrolments.AnyAsync(e => e.ParticipantID == participantId && e.CategoryID == categoryId)) return BadRequest("Already enrolled.");
            if (await _context.Enrolments.CountAsync(e => e.CategoryID == categoryId) >= category.MaxParticipants) return BadRequest("Category full.");

            var enrolment = new Enrolment { ParticipantID = participantId, CategoryID = categoryId, Status = "Confirmed" };
            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return Ok(new { enrolment.EnrolmentID, enrolment.CategoryID, enrolment.Status, enrolment.EnrolmentDate });
        }

        /// <summary>
        /// List the logged-in participant's own enrolments.
        /// </summary>
        /// <returns>A list of the user's enrolments with event and category names.</returns>
        /// <response code="200">Returns the user's enrolments.</response>
        [HttpGet("users/me/enrolments")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEnrolments() => Ok(await _context.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event).Where(e => e.ParticipantID == GetUserId()).Select(e => new { e.EnrolmentID, EventName = e.Category.Event.EventName, CategoryName = e.Category.CategoryName, e.Status }).ToListAsync());

        /// <summary>
        /// Cancel an enrolment.
        /// </summary>
        /// <param name="id">The ID of the enrolment to cancel.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">If the enrolment was cancelled.</response>
        /// <response code="403">If the user is not the owner of the enrolment.</response>
        [HttpDelete("enrolments/{id}")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null || enrolment.ParticipantID != GetUserId()) return Forbid();
            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();
            return Ok("Cancelled.");
        }

        /// <summary>
        /// Capture a result for a participant's enrolment (Organiser only).
        /// </summary>
        /// <param name="id">The ID of the enrolment.</param>
        /// <param name="dto">The result details (FinishTime, Position, Status).</param>
        /// <returns>The captured result object.</returns>
        /// <response code="200">Returns the captured result.</response>
        /// <response code="400">If a result already exists for this enrolment.</response>
        /// <response code="403">If the user is not the event organiser.</response>
        [HttpPost("enrolments/{id}/results")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CaptureResult(int id, [FromBody] ResultDto dto)
        {
            var enrolment = await _context.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event).FirstOrDefaultAsync(e => e.EnrolmentID == id);
            if (enrolment == null || enrolment.Category.Event.OrganiserID != GetUserId()) return Forbid();
            if (await _context.Results.AnyAsync(r => r.EnrolmentID == id)) return BadRequest("Result exists.");

            var result = new Result { EnrolmentID = id, FinishTime = dto.FinishTime, Position = dto.Position, Status = dto.Status };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();
            return Ok(result);
        }
    }
}