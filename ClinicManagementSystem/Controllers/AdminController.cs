using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        // LABEL: Constructor
        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // LABEL: Dashboard
        public IActionResult Dashboard()
        {
            try
            {
                var model = new AdminDashboardViewModel
                {
                    TotalDoctors = _context.Doctors.Count(d => d.IsActive),
                    TotalPatients = _context.Patients.Count(),
                    TotalAppointments = _context.Appointments.Count(),
                    PendingAppointments = _context.Appointments.Count(a => a.Status == "Pending"),
                    RecentAppointments = _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .OrderByDescending(a => a.CreatedAt)
                        .Take(5)
                        .ToList()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        // LABEL: Doctors List
        public async Task<IActionResult> Doctors()
        {
            try
            {
                var doctors = await _context.Doctors
                    .Include(d => d.User)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                return View(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctors");
                TempData["ErrorMessage"] = $"Error loading doctors: {ex.Message}";
                return View(new List<Doctor>());
            }
        }

        // LABEL: Get Add Doctor View
        [HttpGet]
        public IActionResult AddDoctor()
        {
            return View();
        }

        // LABEL: Post Add Doctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(model);
                }

                // Create user account for doctor
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.Name,
                    PhoneNumber = model.Phone
                };

                var result = await _userManager.CreateAsync(user, "Doctor123!");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    var doctor = new Doctor
                    {
                        UserId = user.Id,
                        Name = model.Name,
                        Specialization = model.Specialization,
                        Phone = model.Phone,
                        Address = model.Address,
                        IsActive = true
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Doctor added successfully! Default password: Doctor123!";
                    return RedirectToAction("Doctors");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding doctor");
                ModelState.AddModelError(string.Empty, $"Error adding doctor: {ex.Message}");
            }

            return View(model);
        }

        // LABEL: Get Edit Doctor View
        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DoctorId == id);

                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found!";
                    return RedirectToAction("Doctors");
                }

                var model = new EditDoctorViewModel
                {
                    DoctorId = doctor.DoctorId,
                    Name = doctor.Name,
                    Email = doctor.User.Email,
                    Specialization = doctor.Specialization,
                    Phone = doctor.Phone,
                    Address = doctor.Address,
                    IsActive = doctor.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor for edit");
                TempData["ErrorMessage"] = $"Error loading doctor: {ex.Message}";
                return RedirectToAction("Doctors");
            }
        }

        // LABEL: Post Edit Doctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(EditDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DoctorId == model.DoctorId);

                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found!";
                    return RedirectToAction("Doctors");
                }

                // Check if email is changed and already exists
                if (doctor.User.Email != model.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != doctor.UserId)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        return View(model);
                    }

                    doctor.User.Email = model.Email;
                    doctor.User.UserName = model.Email;
                }

                doctor.Name = model.Name;
                doctor.Specialization = model.Specialization;
                doctor.Phone = model.Phone;
                doctor.Address = model.Address;
                doctor.IsActive = model.IsActive;
                doctor.User.FullName = model.Name;
                doctor.User.PhoneNumber = model.Phone;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor updated successfully!";
                return RedirectToAction("Doctors");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor");
                ModelState.AddModelError(string.Empty, $"Error updating doctor: {ex.Message}");
                return View(model);
            }
        }

        // LABEL: Delete Doctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DoctorId == id);

                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor not found!";
                    return RedirectToAction("Doctors");
                }

                // Check if doctor has any appointments
                var hasAppointments = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == id && (a.Status == "Pending" || a.Status == "Approved"));

                if (hasAppointments)
                {
                    // Soft delete - mark as inactive
                    doctor.IsActive = false;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor marked as inactive. They cannot be permanently deleted because they have active appointments.";
                }
                else
                {
                    // Hard delete - remove doctor record and user account
                    _context.Doctors.Remove(doctor);

                    // Also delete the associated user account
                    var user = await _userManager.FindByIdAsync(doctor.UserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor deleted successfully!";
                }

                return RedirectToAction("Doctors");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting doctor");
                TempData["ErrorMessage"] = $"Error deleting doctor: {ex.Message}";
                return RedirectToAction("Doctors");
            }
        }

        // LABEL: Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found!";
                    return RedirectToAction("SystemUsers");
                }

                // Generate secure random password
                var newPassword = GenerateSecurePassword();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    // Log this security action
                    _logger.LogWarning("Password reset for user {UserId} by admin {AdminUser}",
                        userId, User.Identity.Name);

                    TempData["SuccessMessage"] = $"Password reset successfully! New temporary password: {newPassword}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reset password.";
                }

                return RedirectToAction("SystemUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                TempData["ErrorMessage"] = $"Error resetting password: {ex.Message}";
                return RedirectToAction("SystemUsers");
            }
        }

        // LABEL: Generate Secure Password
        private string GenerateSecurePassword()
        {
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";

            var random = new Random();
            var password = new char[12];

            // Ensure complexity requirements
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Fill remaining characters
            const string allChars = uppercase + lowercase + digits + special;
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the password
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        // LABEL: Patients List
        public async Task<IActionResult> Patients()
        {
            try
            {
                var patients = await _context.Patients
                    .Include(p => p.User)
                    .OrderByDescending(p => p.RegistrationDate)
                    .ToListAsync();
                return View(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patients");
                TempData["ErrorMessage"] = $"Error loading patients: {ex.Message}";
                return View(new List<Patient>());
            }
        }

        // LABEL: Patient Details
        [HttpGet]
        public async Task<IActionResult> PatientDetails(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == id);

                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found!";
                    return RedirectToAction("Patients");
                }

                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();

                var model = new PatientDetailsViewModel
                {
                    Patient = patient,
                    Appointments = appointments
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patient details");
                TempData["ErrorMessage"] = $"Error loading patient details: {ex.Message}";
                return RedirectToAction("Patients");
            }
        }

        // LABEL: Edit Patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(int patientId, string name, int age, string phone, string address, string medicalHistory)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId);

                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found!";
                    return RedirectToAction("Patients");
                }

                patient.Name = name;
                patient.Age = age;
                patient.Phone = phone;
                patient.Address = address;
                patient.MedicalHistory = medicalHistory;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Patient {name} updated successfully!";
                return RedirectToAction("Patients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", patientId);
                TempData["ErrorMessage"] = $"Error updating patient: {ex.Message}";
                return RedirectToAction("Patients");
            }
        }

        // LABEL: Delete Patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatient(int patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .Include(p => p.Appointments)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId);

                if (patient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found!";
                    return RedirectToAction("Patients");
                }

                // Check if patient has any appointments
                if (patient.Appointments.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete patient with existing appointments. Please cancel or reassign appointments first.";
                    return RedirectToAction("Patients");
                }

                // Delete patient record
                _context.Patients.Remove(patient);

                // Also delete the associated user account
                if (patient.User != null)
                {
                    await _userManager.DeleteAsync(patient.User);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Patient {patient.Name} deleted successfully!";
                return RedirectToAction("Patients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", patientId);
                TempData["ErrorMessage"] = $"Error deleting patient: {ex.Message}";
                return RedirectToAction("Patients");
            }
        }

        // LABEL: Appointments List
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointments");
                TempData["ErrorMessage"] = $"Error loading appointments: {ex.Message}";
                return View(new List<Appointment>());
            }
        }

        // LABEL: Update Appointment Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, string status)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("Appointments");
                }

                appointment.Status = status;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Appointment status updated to {status} successfully!";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status");
                TempData["ErrorMessage"] = $"Error updating appointment: {ex.Message}";
                return RedirectToAction("Appointments");
            }
        }

        // LABEL: System Users List
        [HttpGet]
        public async Task<IActionResult> SystemUsers()
        {
            try
            {
                var users = await _userManager.Users
                    .Select(u => new SystemUserViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        Roles = _userManager.GetRolesAsync(u).Result.ToList()
                    })
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading system users");
                TempData["ErrorMessage"] = $"Error loading users: {ex.Message}";
                return View(new List<SystemUserViewModel>());
            }
        }

        // LABEL: Reports View
        [HttpGet]
        public IActionResult Reports()
        {
            try
            {
                var model = new ReportsViewModel
                {
                    TotalAppointmentsByStatus = new Dictionary<string, int>
                    {
                        { "Completed", _context.Appointments.Count(a => a.Status == "Completed") },
                        { "Pending", _context.Appointments.Count(a => a.Status == "Pending") },
                        { "Approved", _context.Appointments.Count(a => a.Status == "Approved") },
                        { "Cancelled", _context.Appointments.Count(a => a.Status == "Cancelled") }
                    },
                    RecentRegistrations = _context.Patients
                        .Include(p => p.User)
                        .OrderByDescending(p => p.RegistrationDate)
                        .Take(5)
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["ErrorMessage"] = $"Error loading reports: {ex.Message}";
                return View(new ReportsViewModel());
            }
        }

        // LABEL: Update User Roles
        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UserRoleUpdateModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRolesAsync(user, model.Roles);

                return Json(new { success = true, message = "Roles updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user roles");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // LABEL: Toggle Doctor Status
        [HttpPost]
        public async Task<IActionResult> ToggleDoctorStatus(int id)
        {
            try
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.DoctorId == id);

                if (doctor == null)
                {
                    return Json(new { success = false, message = "Doctor not found!" });
                }

                doctor.IsActive = !doctor.IsActive;
                await _context.SaveChangesAsync();

                var status = doctor.IsActive ? "activated" : "deactivated";
                return Json(new { success = true, message = $"Doctor {status} successfully!", isActive = doctor.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // LABEL: Export Data
        [HttpPost]
        public async Task<IActionResult> ExportData(string dataType, string format)
        {
            try
            {
                object data = dataType.ToLower() switch
                {
                    "patients" => await _context.Patients.Include(p => p.User).ToListAsync(),
                    "doctors" => await _context.Doctors.Include(d => d.User).Where(d => d.IsActive).ToListAsync(),
                    "appointments" => await _context.Appointments.Include(a => a.Doctor).Include(a => a.Patient).ToListAsync(),
                    _ => null
                };

                if (data == null)
                    return Json(new { success = false, message = "Invalid data type" });

                if (format.ToLower() == "json")
                {
                    return Json(new { success = true, data = data });
                }

                return Json(new { success = false, message = "Format not supported" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // LABEL: Send Bulk Notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulkNotification(string message, string recipientType)
        {
            try
            {
                var users = recipientType.ToLower() switch
                {
                    "patients" => await _userManager.GetUsersInRoleAsync("Patient"),
                    "doctors" => await _userManager.GetUsersInRoleAsync("Doctor"),
                    "all" => _userManager.Users.ToList(),
                    _ => new List<ApplicationUser>()
                };

                // Simulate sending notifications
                // In real application, integrate with email service or push notifications

                TempData["SuccessMessage"] = $"Notification sent to {users.Count} {recipientType}!";
                return RedirectToAction("SystemUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send notification: {ex.Message}";
                return RedirectToAction("SystemUsers");
            }
        }

        // LABEL: Generate Report
        [HttpPost]
        public async Task<IActionResult> GenerateReport(string reportType, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                startDate ??= DateTime.Today.AddMonths(-1);
                endDate ??= DateTime.Today;

                var reportData = new
                {
                    ReportType = reportType,
                    Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                    GeneratedAt = DateTime.Now,
                    Data = new object() // Add actual report data based on type
                };

                return Json(new { success = true, report = reportData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // LABEL: Deactivate User
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // LABEL: Send Bulk Message
        [HttpPost]
        public async Task<IActionResult> SendBulkMessage([FromBody] BulkMessageModel model)
        {
            try
            {
                // Simulate sending messages - in real app, you'd integrate with email/SMS
                TempData["SuccessMessage"] = $"Message sent to {model.UserIds.Count} users!";
                return Json(new { success = true, message = "Messages sent successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // LABEL: Manage Appointments
        [HttpGet]
        public async Task<IActionResult> ManageAppointments()
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointments");
                TempData["ErrorMessage"] = $"Error loading appointments: {ex.Message}";
                return View(new List<Appointment>());
            }
        }
    }
}