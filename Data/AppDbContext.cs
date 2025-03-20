using Microsoft.EntityFrameworkCore;
using Employee_Attendance_Api.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Attendance_Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<WorkHours> WorkHours { get; set; }
        public DbSet<MonthlyWork> MonthlyWorks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("DatabaseGenerated", DatabaseGeneratedOption.Identity);
            });

            modelBuilder.Entity<WorkHours>(entity =>
            {
                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.WorkHours)
                    .HasForeignKey(d => d.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MonthlyWork>(entity =>
            {
                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.MonthlyWorks)
                    .HasForeignKey(d => d.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}