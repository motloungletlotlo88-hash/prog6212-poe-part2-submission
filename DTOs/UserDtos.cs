using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs
{
    public class UpdateProfileDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdatePasswordDto
    {
        [Required] public string CurrentPassword { get; set; } = string.Empty;
        [Required] public string NewPassword { get; set; } = string.Empty;
    }
}