using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs
{
    // FIX: Added EventID to fix Razor view compilation
    public class EventDto
    {
        public int EventID { get; set; }
        [Required] public string EventName { get; set; } = string.Empty;
        [Required] public DateTime EventDate { get; set; }
        [Required] public string Location { get; set; } = string.Empty;
        [Required] public decimal DistanceKM { get; set; }
        public string? EventType { get; set; }
        public string? RouteInfo { get; set; }
        public string? Description { get; set; }
    }

    public class CategoryDto
    {
        [Required] public string CategoryName { get; set; } = string.Empty;
        [Required] public decimal DistanceKM { get; set; }
        public int MaxParticipants { get; set; } = 100;
        public decimal EntryFee { get; set; } = 0;
    }

    public class ResultDto
    {
        [Required] public TimeSpan FinishTime { get; set; }
        [Required] public int Position { get; set; }
        [Required] public string Status { get; set; } = string.Empty;
    }

    // FIX: Strongly typed DTOs replacing List<dynamic> for UI
    public class MyEnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class MyResultDto
    {
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? FinishTime { get; set; }
        public int? Position { get; set; }
    }
}