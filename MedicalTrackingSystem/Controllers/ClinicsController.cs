using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalTrackingSystem.Data;
using MedicalTrackingSystem.Models;

namespace MedicalTrackingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClinicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClinicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetClinics()
        {
            var clinics = await _context.Clinics
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.ClinicId,
                    c.Name,
                    c.Location,
                    c.Description,
                    c.ContactNumber,
                    c.WorkingHours,
                    Doctors = c.Doctors.Select(d => new
                    {
                        d.DoctorId,
                        d.User.FirstName,
                        d.User.LastName,
                        d.User.Email
                    }).ToList()
                })
                .ToListAsync();

            return Ok(clinics);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClinic(int id)
        {
            var clinic = await _context.Clinics
                .Include(c => c.Doctors)
                    .ThenInclude(d => d.User)
                .Where(c => c.ClinicId == id && c.IsActive)
                .Select(c => new
                {
                    c.ClinicId,
                    c.Name,
                    c.Description,
                    c.Location,
                    c.ContactNumber,
                    c.WorkingHours,
                    Doctors = c.Doctors.Select(d => new
                    {
                        d.DoctorId,
                        d.User.FirstName,
                        d.User.LastName,
                        d.User.Email
                    })
                })
                .FirstOrDefaultAsync();

            if (clinic == null)
                return NotFound();

            return Ok(clinic);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateClinic([FromBody] ClinicCreateModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var clinic = new Clinic
            {
                Name = model.Name,
                Description = model.Description,
                Location = model.Location,
                ContactNumber = model.ContactNumber,
                WorkingHours = model.WorkingHours,
                IsActive = true
            };

            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Clinic created successfully", clinicId = clinic.ClinicId });
        }
    }

    public class ClinicCreateModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string ContactNumber { get; set; }
        public string WorkingHours { get; set; }
    }
} 