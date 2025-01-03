using System.ComponentModel.DataAnnotations;

namespace MedicalTrackingSystem.Models
{
    public class Clinic
    {
        [Key]
        public int ClinicId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(20)]
        public string ContactNumber { get; set; }

        [StringLength(100)]
        public string WorkingHours { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Doctor> Doctors { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
} 