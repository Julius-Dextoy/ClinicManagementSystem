using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Patient
    {
        public int PatientId { get; set; }

        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "Unknown";

        [Range(1, 120)]
        public int Age { get; set; } = 0;

        [StringLength(15)]
        public string Phone { get; set; } = "";

        [StringLength(200)]
        public string Address { get; set; } = "Not provided";

        [StringLength(1000)]
        public string MedicalHistory { get; set; } = "No medical history provided";

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public ICollection<Appointment> Appointments { get; set; }
    }
}