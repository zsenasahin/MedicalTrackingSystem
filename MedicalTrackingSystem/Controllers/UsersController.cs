using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalTrackingSystem.Data;
using MedicalTrackingSystem.Models;
using System.Security.Claims;

namespace MedicalTrackingSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userType = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userType == "Patient")
            {
                var profile = await _context.Patients
                    .Include(p => p.User)
                    .Where(p => p.UserId == userId)
                    .Select(p => new
                    {
                        p.User.FirstName,
                        p.User.LastName,
                        p.User.Email,
                        p.DateOfBirth,
                        p.Gender,
                        p.BloodType,
                        p.Weight,
                        p.Height
                    })
                    .FirstOrDefaultAsync();

                return Ok(profile);
            }
            else if (userType == "Doctor")
            {
                var profile = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Clinic)
                    .Where(d => d.UserId == userId)
                    .Select(d => new
                    {
                        d.User.FirstName,
                        d.User.LastName,
                        d.User.Email,
                        Clinic = new
                        {
                            d.Clinic.Name,
                            d.Clinic.Location
                        }
                    })
                    .FirstOrDefaultAsync();

                return Ok(profile);
            }

            return BadRequest("Invalid user type");
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userType = User.FindFirst(ClaimTypes.Role)?.Value;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.Email = model.Email;

            if (userType == "Patient")
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
                if (patient != null)
                {
                    patient.DateOfBirth = model.DateOfBirth;
                    patient.Gender = model.Gender;
                    patient.BloodType = model.BloodType;
                    patient.Weight = model.Weight;
                    patient.Height = model.Height;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("list")]
        public async Task<IActionResult> GetUsers([FromQuery] string userType)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(userType))
            {
                query = query.Where(u => u.UserType == userType);
            }

            var users = await query
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.UserType,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }
    }

    public class ProfileUpdateModel
    {
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsuranceNumber { get; set; }
    }
} 