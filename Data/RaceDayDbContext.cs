using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Enrolment>().HasIndex(e => new { e.ParticipantID, e.CategoryID }).IsUnique();
            modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentID).IsUnique();

            var seedDate = new DateTime(2026, 1, 1);

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Organiser" },
                new Role { RoleID = 2, RoleName = "Participant" }
            );

            modelBuilder.Entity<User>().HasData(
                new User { UserID = 1, RoleID = 1, FullName = "Thandiwe Mokoena", Email = "thandiwe.mokoena@gmail.co.za", PasswordHash = "$2a$11$wWq7tPZ4fX.7yH4v5qT9deF6z3vB5X.2zP8kM4yL5wX6Y7Z8A9B0", PhoneNumber = "0821234567", CreatedAt = seedDate },
                new User { UserID = 2, RoleID = 2, FullName = "Lindiwe Dlamini", Email = "lindiwe.dlamini@gmail.com", PasswordHash = "$2a$11$wWq7tPZ4fX.7yH4v5qT9deF6z3vB5X.2zP8kM4yL5wX6Y7Z8A9B0", PhoneNumber = "0731112222", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Event>().HasData(
                new Event { EventID = 1, OrganiserID = 1, EventName = "Joburg City Marathon", EventDate = new DateTime(2026, 11, 8), Location = "Johannesburg", DistanceKM = 42.2m, EventType = "Run", Description = "Annual road marathon.", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryID = 1, EventID = 1, CategoryName = "42km Marathon", DistanceKM = 42.20m, MaxParticipants = 2000, EntryFee = 350.00m },
                new Category { CategoryID = 2, EventID = 1, CategoryName = "21km Half Marathon", DistanceKM = 21.10m, MaxParticipants = 3000, EntryFee = 250.00m }
            );
        }
    }
}