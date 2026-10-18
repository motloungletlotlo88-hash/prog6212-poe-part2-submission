using System.ComponentModel.DataAnnotations;
namespace RaceDay.Models
{
    public class Role
    {
        [Key] public int RoleID { get; set; }
        [Required, MaxLength(20)] public string RoleName { get; set; } = string.Empty;
    }
}