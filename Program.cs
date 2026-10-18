using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Data;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration (SQLite fallback for cross-platform)
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
if (dbProvider == "Sqlite")
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=raceday.db"));
}
else
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// HttpClient for MVC to call the API (Bypasses dev cert issues locally)
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7018/"); // Ensure this matches your launchSettings
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});

// JWT & Cookie Authentication Setup
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASecretKeyForRaceDayAPI12345!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaceDayAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RaceDayUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "RaceDayScheme";
    options.DefaultAuthenticateScheme = "RaceDayScheme";
    options.DefaultChallengeScheme = "RaceDayScheme";
})
.AddPolicyScheme("RaceDayScheme", "Cookie or JWT", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var auth = context.Request.Headers["Authorization"].ToString();
        return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? JwtBearerDefaults.AuthenticationScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    };
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Controllers & JSON Cycle Handling
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration with XML Comments
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "The backend API powering the RaceDay event management system."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] {} }
    });
});

var app = builder.Build();

// Auto-create database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.Run();