using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RaceDay.Models
{
    public class Category
    {
        [Key] public int CategoryID { get; set; }
        public int EventID { get; set; }
        [ForeignKey("EventID")] public Event? Event { get; set; }
        [Required, MaxLength(100)] public string CategoryName { get; set; } = string.Empty;
        [Column(TypeName = "decimal(6,2)")] public decimal DistanceKM { get; set; }
        public int MaxParticipants { get; set; } = 100;
        [Column(TypeName = "decimal(8,2)")] public decimal EntryFee { get; set; } = 0;
        public ICollection<Enrolment>? Enrolments { get; set; }
    }
}