using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using RaceDay.DTOs;

namespace RaceDay.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AccountController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet] public IActionResult Login() => View("~/Views/Account/Login.cshtml");
        [HttpGet] public IActionResult Register() => View("~/Views/Account/Register.cshtml");

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid) return View("~/Views/Account/Register.cshtml", dto);
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.PostAsJsonAsync("api/auth/register", dto);
            if (response.IsSuccessStatusCode) return await Login(new LoginDto { Email = dto.Email, Password = dto.Password });

            ModelState.AddModelError("", "Registration failed. Email might exist.");
            return View("~/Views/Account/Register.cshtml", dto);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid) return View("~/Views/Account/Login.cshtml", dto);
            var client = _httpClientFactory.CreateClient("RaceDayApi");
            var response = await client.PostAsJsonAsync("api/auth/login", dto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, result!.FullName),
                    new Claim(ClaimTypes.Role, result.Role),
                    new Claim("JwtToken", result.Token)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                return RedirectToAction("Index", "EventsUI");
            }
            ModelState.AddModelError("", "Invalid login.");
            return View("~/Views/Account/Login.cshtml", dto);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "EventsUI");
        }
    }
    public class LoginResponseDto { public string Token { get; set; } = ""; public string FullName { get; set; } = ""; public string Role { get; set; } = ""; }
}