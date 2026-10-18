using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;
using System.Security.Claims;

namespace RaceDay.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public EventsController(RaceDayDbContext context) => _context = context;

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int id) ? id : 0;
        }

        /// <summary>
        /// List/browse all upcoming events.
        /// </summary>
        /// <returns>A list of basic event details.</returns>
        /// <response code="200">Returns the list of events.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events.Select(e => new EventDto
            {
                EventID = e.EventID,
                EventName = e.EventName,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKM = e.DistanceKM,
                EventType = e.EventType
            }).ToListAsync();
            return Ok(events);
        }

        /// <summary>
        /// Get full details of one specific event, including its categories and participants.
        /// </summary>
        /// <param name="id">The ID of the event.</param>
        /// <returns>The full event object.</returns>
        /// <response code="200">Returns the event details.</response>
        /// <response code="404">If the event is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
                .ThenInclude(e => e.Participant)
                .FirstOrDefaultAsync(e => e.EventID == id);
            if (ev == null) return NotFound("Event not found.");
            return Ok(ev);
        }

        /// <summary>
        /// Create a new event.
        /// </summary>
        /// <param name="dto">The event details (Name, Date, Location, Distance, Type, Route, Description).</param>
        /// <returns>The newly created event object.</returns>
        /// <response code="200">Returns the created event.</response>
        /// <response code="403">If the user is not an Organiser.</response>
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
        {
            var ev = new Event
            {
                OrganiserID = GetUserId(),
                EventName = dto.EventName,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKM = dto.DistanceKM,
                EventType = dto.EventType,
                RouteInfo = dto.RouteInfo,
                Description = dto.Description
            };
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        /// <summary>
        /// Update an existing event you organise.
        /// </summary>
        /// <param name="id">The ID of the event to update.</param>
        /// <param name="dto">The updated event details.</param>
        /// <returns>The updated event object.</returns>
        /// <response code="200">Returns the updated event.</response>
        /// <response code="403">If the user is not the event owner or not an Organiser.</response>
        /// <response code="404">If the event is not found.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");
            if (ev.OrganiserID != GetUserId()) return Forbid("You are not the owner of this event.");

            ev.EventName = dto.EventName; ev.EventDate = dto.EventDate; ev.Location = dto.Location;
            ev.DistanceKM = dto.DistanceKM; ev.EventType = dto.EventType; ev.RouteInfo = dto.RouteInfo; ev.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        /// <summary>
        /// Delete an event you organise.
        /// </summary>
        /// <param name="id">The ID of the event to delete.</param>
        /// <returns>A success message.</returns>
        /// <response code="200">If the event was deleted.</response>
        /// <response code="403">If the user is not the event owner.</response>
        /// <response code="404">If the event is not found.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();
            if (ev.OrganiserID != GetUserId()) return Forbid();
            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return Ok("Event deleted.");
        }

        /// <summary>
        /// Define an age or distance category for a specific event.
        /// </summary>
        /// <param name="eventId">The ID of the event to add the category to.</param>
        /// <param name="dto">The category details (Name, Distance, MaxParticipants, Fee).</param>
        /// <returns>The newly created category.</returns>
        /// <response code="200">Returns the created category.</response>
        /// <response code="403">If the user is not the event owner.</response>
        [HttpPost("{eventId}/categories")]
        [Authorize(Roles = "Organiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null || ev.OrganiserID != GetUserId()) return Forbid();

            var category = new Category { EventID = eventId, CategoryName = dto.CategoryName, DistanceKM = dto.DistanceKM, MaxParticipants = dto.MaxParticipants, EntryFee = dto.EntryFee };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }
    }
}