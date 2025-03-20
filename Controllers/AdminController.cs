using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Employee_Attendance_Api.Data;
using Employee_Attendance_Api.Models;
using Employee_Attendance_Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Employee_Attendance_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Dolgozók hozzáadása admin oldalon
        [Authorize(Roles = "Admin")]
        [HttpPost("add-employee")]
        public async Task<IActionResult> AddEmployee([FromBody] RegisterRequest request)
        {
            _logger.LogInformation($"Új felhasználó hozzáadása: {request.Username}");

            // Check if username already exists
            if (await _context.Employees.AnyAsync(e => e.Username == request.Username))
            {
                return BadRequest("A felhasználónév már foglalt!");
            }

            var newEmployee = new Employee
            {
                Name = request.Name,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsAdmin = request.IsAdmin
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Felhasználó sikeresen hozzáadva!", IsAdmin = newEmployee.IsAdmin });
        }

        [HttpPost("add-work-hours")]
        public async Task<IActionResult> AddWorkHours([FromBody] AddWorkHoursRequest request)
        {
            var employee = await _context.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
            {
                return NotFound("Dolgozó nem található!");
            }

            var workHours = new WorkHours
            {
                EmployeeId = request.EmployeeId,
                CheckIn = request.CheckIn,
                CheckOut = request.CheckOut
            };

            _context.WorkHours.Add(workHours);
            await _context.SaveChangesAsync();

            return Ok("Munkaórák sikeresen hozzáadva!");
        }

        public class AddWorkHoursRequest
        {
            public int EmployeeId { get; set; }
            public DateTime CheckIn { get; set; }
            public DateTime? CheckOut { get; set; }
        }     

        // Dolgozók törlése admin oldalon
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete-employee/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound("Dolgozó nem található!");
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok("Dolgozó sikeresen törölve!");
        }

        // Dolgozó adatainak módosítása
        [Authorize(Roles = "Admin")]
        [HttpPut("update-employee/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound("Dolgozó nem található!");
            }

            employee.Name = request.Name;
            employee.Username = request.Username;
            employee.IsAdmin = request.IsAdmin;

            await _context.SaveChangesAsync();

            return Ok("Dolgozó sikeresen módosítva!");
        }

        // Dolgozó jelszavának megváltoztatása
        [Authorize(Roles = "Admin")]
        [HttpPut("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound("Dolgozó nem található!");
            }

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Jelszó sikeresen megváltoztatva!");
        }

        //Ez új, ez ahhoz kell hogy az admin oldalon megjelenítse az adatbázisban szereplő Dolgozókat (és Adminokat!) is.
        //Az isAdmin alapján kell majd eldönteni hogy bejelentkezéskor az admin oldalra irányítson át vagy a dolgozi Dashboard oldalra.
        [Authorize(Roles = "Admin")]
        [HttpGet("get-employees-data-by-id")]
        public async Task<IActionResult> GetEmployees([FromQuery] int? id)
        {
            if (id.HasValue)
            {
                var employee = await _context.Employees
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Username,
                        d.IsAdmin
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return NotFound(new { Message = "Dolgozó nem található!" });
                }

                return Ok(employee);
            }

            var employees = await _context.Employees
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Username,
                    d.IsAdmin
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("employees-all")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployeesList()
        {
            var employees = await _context.Employees.ToListAsync();
            return Ok(employees);
        }

        [HttpGet("employee/{id}")]
        public async Task<ActionResult<Employee>> GetEmployeeDetails(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound("Dolgozó nem található!");
            }

            return Ok(employee);
        }

        [HttpGet("monthly-report/{employeeId}/{year}/{month}")]
        public async Task<ActionResult<object>> GetMonthlyReport(int employeeId, int year, int month)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return NotFound("Employee not found!");
            }

            var monthlyWork = await _context.MonthlyWorks
                .Where(m => m.EmployeeId == employeeId && 
                           m.Date.Year == year && 
                           m.Date.Month == month)
                .FirstOrDefaultAsync();

            if (monthlyWork == null)
            {
                return NotFound("Nem található adat a megadott hónapra!");
            }

            var dailyStats = await _context.WorkHours
                .Where(w => w.EmployeeId == employeeId &&
                           w.CheckIn.Year == year &&
                           w.CheckIn.Month == month)
                .Select(w => new
                {
                    Date = w.CheckIn.Date,
                    CheckIn = w.CheckIn,
                    CheckOut = w.CheckOut,
                    WorkedMinutes = w.CheckOut.HasValue ? 
                        (int)(w.CheckOut.Value - w.CheckIn).TotalMinutes : 0
                })
                .ToListAsync();

            return Ok(new
            {
                EmployeeId = employeeId,
                EmployeeName = employee.Name,
                Year = year,
                Month = month,
                TotalWorkedMinutes = monthlyWork.WorkedMinutes,
                DailyStats = dailyStats
            });
        }
    }

    public class UpdateEmployeeRequest
    {
        public required string Name { get; set; }
        public required string Username { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string NewPassword { get; set; }
    }
}
