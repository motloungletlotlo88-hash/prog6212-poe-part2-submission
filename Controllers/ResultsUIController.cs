using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.DTOs;

namespace RaceDay.Controllers
{
    [Authorize]
    public class ResultsUIController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ResultsUIController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        private HttpClient GetApiClient()
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", User.FindFirst("JwtToken")?.Value);
            return client;
        }

        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> MyResults()
        {
            var response = await GetApiClient().GetAsync("api/users/me/results");
            // FIX: Use strongly-typed DTO instead of dynamic
            var results = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<MyResultDto>>()
                : new List<MyResultDto>();
            return View("~/Views/Results/MyResults.cshtml", results);
        }

        [Authorize(Roles = "Organiser"), HttpGet]
        public IActionResult CaptureResult(int enrolmentId, string participantName)
        {
            ViewBag.EnrolmentId = enrolmentId;
            ViewBag.ParticipantName = participantName;
            return View("~/Views/Results/CaptureResult.cshtml");
        }

        [Authorize(Roles = "Organiser"), HttpPost]
        public async Task<IActionResult> CaptureResult(int enrolmentId, ResultDto dto)
        {
            if (!ModelState.IsValid) return View("~/Views/Results/CaptureResult.cshtml", dto);
            var response = await GetApiClient().PostAsJsonAsync($"api/enrolments/{enrolmentId}/results", dto);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result captured!";
                return RedirectToAction("Index", "EventsUI");
            }
            ModelState.AddModelError("", "Failed to capture.");
            return View("~/Views/Results/CaptureResult.cshtml", dto);
        }
    }
}