using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Employee_Attendance_Api.Models
{
    [Table("Employees")]
    public class Employee
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        
        [Required]
        [Column("Name")]
        public required string Name { get; set; }
        
        [Required]
        [Column("Username")]
        public required string Username { get; set; }
        
        [Required]
        [Column("PasswordHash")]
        public required string PasswordHash { get; set; }
        
        [Column("IsAdmin")]
        public bool IsAdmin { get; set; }

        // Navigation properties
        public ICollection<WorkHours> WorkHours { get; set; } = new List<WorkHours>();
        public ICollection<MonthlyWork> MonthlyWorks { get; set; } = new List<MonthlyWork>();
    }
}
