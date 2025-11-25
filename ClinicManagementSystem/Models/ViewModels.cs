using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    // LABEL: Admin Dashboard View Model
    public class AdminDashboardViewModel
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public List<Appointment> RecentAppointments { get; set; } = new List<Appointment>();
    }

    // LABEL: Doctor View Model
    public class DoctorViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Specialization is required")]
        [Display(Name = "Specialization")]
        [StringLength(50, ErrorMessage = "Specialization cannot exceed 50 characters")]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }
    }

    // LABEL: Edit Doctor View Model
    public class EditDoctorViewModel
    {
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Specialization is required")]
        [Display(Name = "Specialization")]
        [StringLength(50, ErrorMessage = "Specialization cannot exceed 50 characters")]
        public string Specialization { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;
    }

    // LABEL: Doctor Dashboard View Model
    public class DoctorDashboardViewModel
    {
        public Doctor Doctor { get; set; }
        public List<Appointment> TodayAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
    }

    // LABEL: Book Appointment View Model
    public class BookAppointmentViewModel
    {
        [Required(ErrorMessage = "Please select a doctor")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Please select an appointment date")]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select an appointment time")]
        [Display(Name = "Appointment Time")]
        public TimeSpan AppointmentTime { get; set; }

        [Display(Name = "Notes (Optional)")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }

        public List<Doctor> Doctors { get; set; } = new List<Doctor>();
        public List<TimeSpan> AvailableTimeSlots { get; set; } = new List<TimeSpan>();
    }

    // LABEL: Login View Model
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    // LABEL: Register Patient View Model
    public class RegisterPatientViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Age is required")]
        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get; set; }

        [Display(Name = "Medical History (Optional)")]
        [StringLength(1000, ErrorMessage = "Medical history cannot exceed 1000 characters")]
        public string MedicalHistory { get; set; }
    }

    // LABEL: Patient Details View Model
    public class PatientDetailsViewModel
    {
        public Patient Patient { get; set; }
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    // LABEL: System User View Model
    public class SystemUserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    // LABEL: Reports View Model
    public class ReportsViewModel
    {
        public Dictionary<string, int> TotalAppointmentsByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AppointmentsByDoctor { get; set; } = new Dictionary<string, int>();
        public List<Patient> RecentRegistrations { get; set; } = new List<Patient>();
    }

    // LABEL: Error View Model
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string Message { get; set; }
        public int? StatusCode { get; set; }
    }
}