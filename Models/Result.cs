using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace RaceDay.Models
{
    public class Result
    {
        [Key] public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        [ForeignKey("EnrolmentID")] public Enrolment? Enrolment { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        [MaxLength(20)] public string Status { get; set; } = "Finished";
    }
}