using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using System.Security.Claims;

namespace RaceDay.Controllers
{
    [Route("api")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public ResultsController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>
        /// Update a previously captured result.
        /// </summary>
        /// <param name="id">The ID of the result to update.</param>
        /// <param name="dto">The updated result details (FinishTime, Position, Status).</param>
        /// <returns>The updated result object.</returns>
        /// <response code="200">Returns the updated result.</response>
        /// <response code="403">If the user is not the event organiser.</response>
        [HttpPut("results/{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultDto dto)
        {
            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null || result.Enrolment.Category.Event.OrganiserID != GetUserId()) return Forbid();

            result.FinishTime = dto.FinishTime;
            result.Position = dto.Position;
            result.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(result);
        }

        /// <summary>
        /// View the logged-in participant's full personal performance history.
        /// </summary>
        /// <returns>A list of past results (EventName, CategoryName, FinishTime, Position).</returns>
        /// <response code="200">Returns the participant's results.</response>
        [HttpGet("users/me/results")]
        [Authorize(Roles = "Participant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyResults()
        {
            var results = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
                .Where(r => r.Enrolment.ParticipantID == GetUserId())
                .Select(r => new { EventName = r.Enrolment.Category.Event.EventName, CategoryName = r.Enrolment.Category.CategoryName, r.FinishTime, r.Position })
                .ToListAsync();

            return Ok(results);
        }
    }
}