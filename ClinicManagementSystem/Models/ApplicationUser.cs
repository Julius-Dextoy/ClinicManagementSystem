using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Doctor Doctor { get; set; }
        public Patient Patient { get; set; }
    }

    public class MedicalNotesModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        [StringLength(500)]
        public string Notes { get; set; }
    }

    public class UpdateAppointmentModel
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }

    public class UserRoleUpdateModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class BulkMessageModel
    {
        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        public List<string> UserIds { get; set; } = new List<string>();
    }
}