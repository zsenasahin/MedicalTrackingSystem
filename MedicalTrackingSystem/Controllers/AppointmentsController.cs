using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalTrackingSystem.Data;
using MedicalTrackingSystem.Models;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace MedicalTrackingSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var userType = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Clinic)
                .AsQueryable();

            if (userType == "Patient")
            {
                var patientId = await _context.Patients
                    .Where(p => p.UserId == userId)
                    .Select(p => p.PatientId)
                    .FirstOrDefaultAsync();

                query = query.Where(a => a.PatientId == patientId);
            }
            else if (userType == "Doctor")
            {
                var doctorId = await _context.Doctors
                    .Where(d => d.UserId == userId)
                    .Select(d => d.DoctorId)
                    .FirstOrDefaultAsync();

                query = query.Where(a => a.DoctorId == doctorId);
            }

            var appointments = await query
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentDate,
                    a.Status,
                    a.Type,
                    Doctor = new
                    {
                        a.Doctor.DoctorId,
                        a.Doctor.User.FirstName,
                        a.Doctor.User.LastName,
                        a.Doctor.User.Email
                    },
                    Patient = new
                    {
                        a.Patient.PatientId,
                        a.Patient.User.FirstName,
                        a.Patient.User.LastName,
                        a.Patient.User.Email
                    },
                    Clinic = new
                    {
                        a.Clinic.ClinicId,
                        a.Clinic.Name,
                        a.Clinic.Location
                    }
                })
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentCreateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get the current user's ID from claims
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                // Get the patient ID for the current user
                var patientId = await _context.Patients
                    .Where(p => p.UserId == userId)
                    .Select(p => p.PatientId)
                    .FirstOrDefaultAsync();

                if (patientId == 0)
                    return BadRequest("Patient not found");

                // Verify that the doctor exists
                var doctorExists = await _context.Doctors
                    .AnyAsync(d => d.DoctorId == model.DoctorId && d.ClinicId == model.ClinicId);

                if (!doctorExists)
                    return BadRequest("Selected doctor not found or does not belong to the selected clinic");

                // Create the appointment
                var appointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = model.DoctorId,
                    ClinicId = model.ClinicId,
                    AppointmentDate = model.AppointmentDate,
                    Type = model.Type ?? "Regular",
                    Symptoms = model.Symptoms ?? "",
                    Status = "Scheduled",
                    CreatedAt = DateTime.UtcNow,
                    Diagnosis = "",
                    Notes = "",
                    Prescription = ""
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Appointment created successfully", 
                    appointmentId = appointment.AppointmentId,
                    appointmentDate = appointment.AppointmentDate,
                    status = appointment.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create appointment: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentUpdateModel model)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound();

            var userType = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (userType == "Doctor")
            {
                var doctorId = await _context.Doctors
                    .Where(d => d.UserId == userId)
                    .Select(d => d.DoctorId)
                    .FirstOrDefaultAsync();

                if (appointment.DoctorId != doctorId)
                    return Forbid();

                appointment.Status = model.Status;
                appointment.Diagnosis = model.Diagnosis;
                appointment.Notes = model.Notes;
            }
            else if (userType == "Patient")
            {
                var patientId = await _context.Patients
                    .Where(p => p.UserId == userId)
                    .Select(p => p.PatientId)
                    .FirstOrDefaultAsync();

                if (appointment.PatientId != patientId)
                    return Forbid();

                if (appointment.Status != "Scheduled")
                    return BadRequest("Cannot modify a confirmed appointment");

                appointment.Symptoms = model.Symptoms;
            }

            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound();

            var userType = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (userType == "Patient")
            {
                var patientId = await _context.Patients
                    .Where(p => p.UserId == userId)
                    .Select(p => p.PatientId)
                    .FirstOrDefaultAsync();

                if (appointment.PatientId != patientId)
                    return Forbid();
            }

            if (appointment.Status != "Scheduled")
                return BadRequest("Cannot cancel a confirmed appointment");

            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled successfully" });
        }
    }

    public class AppointmentCreateModel
    {
        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int ClinicId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        public string? Type { get; set; }

        public string? Symptoms { get; set; }
    }

    public class AppointmentUpdateModel
    {
        public string Status { get; set; }
        public string Diagnosis { get; set; }
        public string Notes { get; set; }
        public string Symptoms { get; set; }
    }
} 