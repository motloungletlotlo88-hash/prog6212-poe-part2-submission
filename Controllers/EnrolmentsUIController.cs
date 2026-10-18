using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.DTOs;

namespace RaceDay.Controllers
{
    [Authorize(Roles = "Participant")]
    public class EnrolmentsUIController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public EnrolmentsUIController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        private HttpClient GetApiClient()
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", User.FindFirst("JwtToken")?.Value);
            return client;
        }

        public async Task<IActionResult> MyEnrolments()
        {
            var response = await GetApiClient().GetAsync("api/users/me/enrolments");
            // FIX: Use strongly-typed DTO instead of dynamic
            var enrolments = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<MyEnrolmentDto>>()
                : new List<MyEnrolmentDto>();
            return View("~/Views/Enrolments/MyEnrolments.cshtml", enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            var response = await GetApiClient().PostAsync($"api/categories/{categoryId}/enrolments", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Successfully enrolled!";
                return RedirectToAction(nameof(MyEnrolments));
            }
            TempData["Error"] = await response.Content.ReadAsStringAsync();
            return RedirectToAction("Details", "EventsUI", new { id = eventId });
        }
    }
}