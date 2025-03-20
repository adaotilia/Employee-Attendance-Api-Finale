using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Attendance_Api.Models
{
    [Table("WorkHours")]
    public class WorkHours
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("EmployeeId")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Column("CheckIn")]
        public DateTime CheckIn { get; set; }

        [Column("CheckOut")]
        public DateTime? CheckOut { get; set; }
    }
}
