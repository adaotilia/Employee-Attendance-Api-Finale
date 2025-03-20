using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Employee_Attendance_Api.Data;
using Employee_Attendance_Api.Models;
using Employee_Attendance_Api.Services;

namespace Employee_Attendance_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, IJwtService jwtService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation($"Bejelentkezés kipróbálása: {request.Username}");
            
            var employee = await _context.Employees
                .FirstOrDefaultAsync(d => d.Username == request.Username);

            if (employee == null)
            {
                _logger.LogWarning($"Felhasználó nem található: {request.Username}");
                return Unauthorized("Érvénytelen felhasználónév vagy jelszó!");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
            {
                _logger.LogWarning($"Érvénytelen jelszót adott meg: {request.Username}");
                return Unauthorized("Érvénytelen felhasználónév vagy jelszó!");
            }

            var token = _jwtService.GenerateToken(employee);

            _logger.LogInformation($"Sikeres bejelentkezés: {request.Username}");

            return Ok(new { 
                Token = token,
                EmployeeId = employee.Id,
                Name = employee.Name,
                IsAdmin = employee.IsAdmin
            });
        }

        [HttpPost("setup-admin")]
        public async Task<IActionResult> SetupAdmin([FromBody] LoginRequest request)
        {
            try 
            {
                _logger.LogInformation("Admin létrehozásának kipróbálása...");
                
                // Ellenőrizzük, hogy van-e már felhasználó az adatbázisban
                if (await _context.Employees.AnyAsync())
                {
                    _logger.LogWarning("Admin létrehozása sikertelen: az adatbázis nem üres");
                    return BadRequest("Az első admin már létrehozva!");
                }

                var admin = new Employee
                {
                    Name = "Admin",
                    Username = request.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    IsAdmin = true
                };

                _context.Employees.Add(admin);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin felhasználó sikeresen létrehozva: {request.Username}");

                var token = _jwtService.GenerateToken(admin);

                return Ok(new { 
                    Token = token,
                    Message = "Admin felhasználó sikeresen létrehozva!",
                    EmployeeId = admin.Id,
                    Name = admin.Name,
                    IsAdmin = admin.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin létrehozása sikertelen");
                return StatusCode(500, new { Message = "Hiba történt az admin létrehozása során!", Error = ex.Message });
            }
        }
    }

    public class RegisterRequest
    {
        public required string Name { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
