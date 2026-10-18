using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Models
{
    public class User
    {
        [Key] public int UserID { get; set; }
        public int RoleID { get; set; }
        [ForeignKey("RoleID")] public Role? Role { get; set; }
        [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
        [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        [JsonIgnore] // FIX: Prevents BCrypt hash from being sent to client
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)] public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Event>? Events { get; set; }
        public ICollection<Enrolment>? Enrolments { get; set; }
    }
}