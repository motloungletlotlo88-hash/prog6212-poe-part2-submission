using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RaceDay.Models
{
    public class Enrolment
    {
        [Key] public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        [ForeignKey("ParticipantID")] public User? Participant { get; set; }
        public int CategoryID { get; set; }
        [ForeignKey("CategoryID")] public Category? Category { get; set; }
        public DateTime EnrolmentDate { get; set; } = DateTime.Now;
        [MaxLength(20)] public string Status { get; set; } = "Confirmed";
        public Result? Result { get; set; }
    }
}