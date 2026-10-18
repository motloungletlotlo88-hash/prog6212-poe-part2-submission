using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    public class EventsUIController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public EventsUIController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        private HttpClient GetApiClient()
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var token = User.FindFirst("JwtToken")?.Value;
            if (!string.IsNullOrEmpty(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> Index()
        {
            var response = await GetApiClient().GetAsync("api/events");
            var events = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<EventDto>>() : new List<EventDto>();
            return View("~/Views/Events/Index.cshtml", events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var response = await GetApiClient().GetAsync($"api/events/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();
            return View("~/Views/Events/Details.cshtml", await response.Content.ReadFromJsonAsync<Event>());
        }

        [Authorize(Roles = "Organiser")]
        public IActionResult Create() => View("~/Views/Events/Create.cshtml");

        [HttpPost, Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Create(EventDto dto)
        {
            if (!ModelState.IsValid) return View("~/Views/Events/Create.cshtml", dto);
            var response = await GetApiClient().PostAsJsonAsync("api/events", dto);
            if (response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));
            ModelState.AddModelError("", "Failed to create.");
            return View("~/Views/Events/Create.cshtml", dto);
        }

        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Manage(int id)
        {
            var response = await GetApiClient().GetAsync($"api/events/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();
            return View("~/Views/Events/Manage.cshtml", await response.Content.ReadFromJsonAsync<Event>());
        }

        [HttpPost, Authorize(Roles = "Organiser")]
        public async Task<IActionResult> AddCategory(CategoryDto dto, int EventId)
        {
            var response = await GetApiClient().PostAsJsonAsync($"api/events/{EventId}/categories", dto);
            if (response.IsSuccessStatusCode) TempData["Success"] = "Category added!";
            return RedirectToAction(nameof(Manage), new { id = EventId });
        }
    }
}