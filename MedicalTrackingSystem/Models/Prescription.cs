using System.ComponentModel.DataAnnotations;

namespace MedicalTrackingSystem.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string MedicationName { get; set; }

        [StringLength(50)]
        public string Dosage { get; set; }

        [StringLength(50)]
        public string Frequency { get; set; }

        [StringLength(50)]
        public string Duration { get; set; }

        [StringLength(500)]
        public string Instructions { get; set; }

        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiryDate { get; set; }

        // Navigation property
        public virtual Appointment Appointment { get; set; }
    }
} 