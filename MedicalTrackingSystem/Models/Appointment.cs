using System.ComponentModel.DataAnnotations;

namespace MedicalTrackingSystem.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int ClinicId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";

        [StringLength(50)]
        public string Type { get; set; }

        [StringLength(500)]
        public string Symptoms { get; set; }

        [StringLength(500)]
        public string Diagnosis { get; set; }

        public string Prescription { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }
        public virtual Clinic Clinic { get; set; }
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; }
    }
} 