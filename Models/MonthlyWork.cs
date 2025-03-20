using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Attendance_Api.Models
{
    [Table("MonthlyWorks")]
    public class MonthlyWork
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("EmployeeId")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public required Employee Employee { get; set; }

        [Column("Date")]
        public DateTime Date { get; set; }

        [Column("WorkedMinutes")]
        public int WorkedMinutes { get; set; }
    }
}
