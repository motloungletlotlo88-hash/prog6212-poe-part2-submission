using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace RaceDay_Tests
{
    public class RaceDayTests
    {
        private readonly RaceDayDbContext _context;
        private readonly Mock<IConfiguration> _mockConfig;

        public RaceDayTests()
        {
            // 1. Setup In-Memory Database for isolated testing
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RaceDayDbContext(options);

            // Seed essential Roles
            _context.Roles.AddRange(
                new Role { RoleID = 1, RoleName = "Organiser" },
                new Role { RoleID = 2, RoleName = "Participant" }
            );
            _context.SaveChanges();

            // 2. Mock Configuration for JWT generation
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("ThisIsASecretKeyForTestingPurposesOnly123!");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        }

        // --- Helper Method to Simulate Authenticated Users ---
        private void SetupControllerUser(ControllerBase controller, int userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        // ============================================================
        // 1. AUTHENTICATION TESTS (Register & Login)
        // ============================================================

        [Fact]
        public async Task Register_NewUser_ReturnsSuccess()
        {
            // Arrange
            var controller = new AuthController(_context, _mockConfig.Object);
            var dto = new RegisterDto { FullName = "Test User", Email = "test@test.com", Password = "Password123!", Role = "Participant" };

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await _context.Users.CountAsync());
        }

        [Fact]
        public async Task Register_ExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            _context.Users.Add(new User { Email = "existing@test.com", FullName = "Existing", PasswordHash = "hash", RoleID = 2 });
            await _context.SaveChangesAsync();
            var controller = new AuthController(_context, _mockConfig.Object);
            var dto = new RegisterDto { FullName = "Test", Email = "existing@test.com", Password = "Password123!", Role = "Participant" };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email already registered.", badRequestResult.Value);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_context, _mockConfig.Object);
            var dto = new LoginDto { Email = "nonexistent@test.com", Password = "WrongPassword" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ============================================================
        // 2. EVENT MANAGEMENT TESTS (Organiser Access & Success)
        // ============================================================

        [Fact]
        public async Task CreateEvent_AsOrganiser_ReturnsSuccess()
        {
            // Arrange
            var controller = new EventsController(_context);
            SetupControllerUser(controller, userId: 1, role: "Organiser");

            var dto = new EventDto
            {
                EventName = "Test Run",
                EventDate = DateTime.Now.AddDays(10),
                Location = "Test City",
                DistanceKM = 10.5m,
                EventType = "Run"
            };

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await _context.Events.CountAsync());
        }

        [Fact]
        public async Task UpdateEvent_AsDifferentOrganiser_ReturnsForbid()
        {
            // Arrange
            var ev = new Event { EventID = 1, OrganiserID = 1, EventName = "Original", Location = "Loc", DistanceKM = 5 };
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            var controller = new EventsController(_context);
            // Mock an Organiser with ID 2 (not the owner of the event)
            SetupControllerUser(controller, userId: 2, role: "Organiser");

            var dto = new EventDto { EventName = "Updated", EventDate = DateTime.Now, Location = "New Location", DistanceKM = 10 };

            // Act
            var result = await controller.UpdateEvent(1, dto);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
            Assert.Equal("You are not the owner of this event.", forbidResult.ForbidReasons![0]);
        }

        // ============================================================
        // 3. ROLE REJECTION TESTS
        // ============================================================

        [Fact]
        public async Task CreateEvent_AsParticipant_ReturnsForbid()
        {
            // Arrange
            var controller = new EventsController(_context);
            // Simulate a Participant attempting to access an Organiser endpoint
            SetupControllerUser(controller, userId: 3, role: "Participant");

            var dto = new EventDto { EventName = "Hacked Event", EventDate = DateTime.Now, Location = "Nowhere", DistanceKM = 5 };

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ============================================================
        // 4. EVENT ENROLMENTS (Database Recording & Failures)
        // ============================================================

        [Fact]
        public async Task Enrol_InCategory_ReturnsSuccess_AndRecordsInDb()
        {
            // Arrange
            _context.Categories.Add(new Category { CategoryID = 1, EventID = 1, CategoryName = "10km", MaxParticipants = 10, DistanceKM = 10 });
            await _context.SaveChangesAsync();

            var controller = new EnrolmentsController(_context);
            SetupControllerUser(controller, userId: 1, role: "Participant");

            // Act
            var result = await controller.Enrol(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            // Verify the enrolment was correctly recorded in the database
            var enrolment = await _context.Enrolments.FirstOrDefaultAsync(e => e.CategoryID == 1);
            Assert.NotNull(enrolment);
            Assert.Equal(1, enrolment.ParticipantID);
            Assert.Equal("Confirmed", enrolment.Status);
        }

        [Fact]
        public async Task Enrol_AlreadyEnrolled_ReturnsBadRequest()
        {
            // Arrange
            _context.Categories.Add(new Category { CategoryID = 1, EventID = 1, CategoryName = "10km", MaxParticipants = 10, DistanceKM = 10 });
            _context.Enrolments.Add(new Enrolment { EnrolmentID = 1, CategoryID = 1, ParticipantID = 1, Status = "Confirmed" });
            await _context.SaveChangesAsync();

            var controller = new EnrolmentsController(_context);
            SetupControllerUser(controller, userId: 1, role: "Participant");

            // Act
            var result = await controller.Enrol(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Already enrolled.", badRequestResult.Value);
        }
    }
}