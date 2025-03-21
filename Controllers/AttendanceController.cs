using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Employee_Attendance_Api.Data;
using Employee_Attendance_Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Employee_Attendance_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }

        // Dolgozó elkezdi a munkát
        [HttpPost("CheckIn")]
        public async Task<ActionResult<WorkHours>> CheckIn()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var existingEntry = await _context.WorkHours
                .Where(w => w.EmployeeId == userId && w.CheckOut == null)
                .FirstOrDefaultAsync();

            if (existingEntry != null)
            {
                return BadRequest("Már bejelentkeztél!");
            }

            var workHours = new WorkHours
            {
                EmployeeId = userId,
                CheckIn = DateTime.Now
            };

            _context.WorkHours.Add(workHours);
            await _context.SaveChangesAsync();

            return Ok(workHours);
        }

        [HttpPost("CheckOut")]
        public async Task<ActionResult<WorkHours>> CheckOut()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var lastEntry = await _context.WorkHours
                .Where(w => w.EmployeeId == userId && w.CheckOut == null)
                .FirstOrDefaultAsync();

            if (lastEntry == null)
            {
                return BadRequest("Nincs aktív bejelentkezés!");
            }

            lastEntry.CheckOut = DateTime.Now;
            var workedMinutes = (int)(lastEntry.CheckOut.Value - lastEntry.CheckIn).TotalMinutes;

            var employee = await _context.Employees.FindAsync(userId);
            if (employee == null)
            {
                return NotFound("Dolgozó nem található!");
            }

            var currentDate = DateTime.Now;
            var monthlyWork = await _context.MonthlyWorks
                .FirstOrDefaultAsync(m => m.EmployeeId == userId && 
                                        m.Date.Month == currentDate.Month && 
                                        m.Date.Year == currentDate.Year);

            if (monthlyWork == null)
            {
                monthlyWork = new MonthlyWork
                {
                    EmployeeId = userId,
                    Employee = employee,
                    Date = new DateTime(currentDate.Year, currentDate.Month, 1),
                    WorkedMinutes = workedMinutes
                };
                _context.MonthlyWorks.Add(monthlyWork);
            }
            else
            {
                monthlyWork.WorkedMinutes += workedMinutes;
            }

            await _context.SaveChangesAsync();

            return Ok(lastEntry);
        }

        // Napi munka lekérdezése
        [HttpGet("Current")]
        public async Task<ActionResult<object>> GetCurrentSession()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var currentSession = await _context.WorkHours
                .Where(w => w.EmployeeId == userId && w.CheckOut == null)
                .FirstOrDefaultAsync();

             if (currentSession == null)
            {
                return NotFound("Nincs aktív munkamenet!");
            }

            var calculator = new WorkHourCalculator();
            var workedTime = calculator.CalculateWorkedTime(currentSession.CheckIn, currentSession.CheckOut);

            return Ok(new
            {
                currentSession.Id,
                currentSession.CheckIn,
                currentSession.CheckOut,
                WorkedTime = workedTime
            });
        }

        // Havi munkaórák lekérdezése
        public class WorkHourCalculator
        {
            public string CalculateWorkedTime(DateTime checkIn, DateTime? checkOut)
            {
                if (!checkOut.HasValue)
                {
                    return "00:00"; // Ha nincs kilépési idő, akkor 00:00
                }
        
                TimeSpan duration = checkOut.Value - checkIn;
                return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}";
            }
        }

        [HttpGet("Monthly")]
        public async Task<ActionResult<object>> GetMonthlyStats()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var currentDate = DateTime.Now;
            var calculator = new WorkHourCalculator();
        
            var monthlyStats = await _context.WorkHours
                .Where(w => w.EmployeeId == userId && 
                           w.CheckIn.Month == currentDate.Month && 
                           w.CheckIn.Year == currentDate.Year)
                .Select(w => new
                {
                    Date = w.CheckIn.Date,
                    CheckIn = w.CheckIn,
                    CheckOut = w.CheckOut,
                    WorkedTime = calculator.CalculateWorkedTime(w.CheckIn, w.CheckOut)
                })
                .ToListAsync();
        
            return Ok(monthlyStats);
        }
    }
}