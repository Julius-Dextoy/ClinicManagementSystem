using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }

        public string UserId { get; set; } // Make optional for initial setup

        public ApplicationUser User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "Unknown Doctor";

        [Required]
        [StringLength(50)]
        public string Specialization { get; set; } = "General";

        [Required]
        [Phone]
        [StringLength(15)]
        public string Phone { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = "Not provided";

        public bool IsActive { get; set; } = true;

        public ICollection<Appointment> Appointments { get; set; }
    }
}