**RaceDay - Event Management System
** 
RaceDay is a comprehensive, full-stack ASP.NET Core application designed to manage running, cycling, and walking events. It features a robust Web API backend and a modern, responsive MVC front-end, allowing Organisers to create events and Participants to enrol and track their results.

This project was developed as part of the PROG6212 Portfolio of Evidence (POE).

Features
Dual Authentication System: Uses JWT Bearer tokens for the REST API and Cookie Authentication for the MVC UI, seamlessly routed via a custom Policy Scheme.

Role-Based Access Control (RBAC): Strict separation between Organiser and Participant functionalities.

Event Management: Organisers can Create, Read, Update, and Delete events. Both roles can browse upcoming events.

Category Definition: Organisers can define specific distance/age categories for each event (e.g., 10km, 42km, Junior).

Enrolments: Participants can seamlessly enrol in specific event categories.

Result Tracking: Organisers can capture finish times and positions; Participants can view their personal performance history.

Live API Documentation: Fully integrated Swagger UI with XML comments and JWT authorization support.

Professional UI: Customized Bootstrap 5 implementation with modern typography, responsive grids, and hover effects.

Tech Stack
Framework: ASP.NET Core (MVC + Web API)

Database: Entity Framework Core (Code-First)

DB Provider: SQL Server (with SQLite fallback for cross-platform testing)

Security: JWT (JSON Web Tokens) & Cookie Authentication, BCrypt.Net for password hashing.

Documentation: Swashbuckle (Swagger)

Testing: xUnit, Moq, EF Core In-Memory Database

Prerequisites
.NET 8 SDK (or newer)

SQL Server (or LocalDB) installed.

Visual Studio 2022 (recommended) or VS Code.

Getting Started
1. Clone the Repository
bash
git clone https://github.com/YourUsername/prog6212-poe-part2-submission.git
cd RaceDay
2. Configure the Database
Open appsettings.json. By default, it is configured for SQL Server LocalDB. If you wish to use SQLite, change the DatabaseProvider to Sqlite.

json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "ThisIsASecretKeyForRaceDayAPI12345!",
    "Issuer": "RaceDayAPI",
    "Audience": "RaceDayUsers"
  }
}
3. Apply Migrations / Create Database
The project is configured to automatically create the database and apply seed data on startup using db.Database.EnsureCreated().

However, if you prefer using migrations, run the following in the Package Manager Console:

powershell
Add-Migration InitialCreate
Update-Database
4. Run the Application
Press F5 in Visual Studio or run the following command:

bash
dotnet run --project RaceDay
Default Test Credentials
The database seeds two default accounts for immediate testing:

Organiser Account:

Email: thandiwe.mokoena@gmail.co.za

Password: Password123!

Participant Account:

Email: lindiwe.dlamini@gmail.com

Password: Password123!

API Documentation (Swagger)
Once the application is running, navigate to:
https://localhost:7018/swagger

All endpoints are documented with descriptions, request bodies, and expected HTTP responses.

To test protected endpoints, log in via /api/auth/login, copy the token, click the Authorize padlock icon, and paste the token.

Running Unit Tests
The solution includes a dedicated xUnit test project (RaceDay.Tests) covering registration, login, role rejection, and database recording.

To run the tests via terminal:

bash
dotnet test
Or, open Test Explorer in Visual Studio and click Run All.
