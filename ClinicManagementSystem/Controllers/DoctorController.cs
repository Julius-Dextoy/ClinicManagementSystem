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
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DoctorController> _logger;

        // LABEL: Constructor
        public DoctorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DoctorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // LABEL: Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogWarning("Doctor profile not found for user: {UserId}", user.Id);
                    return NotFound();
                }

                var model = new DoctorDashboardViewModel
                {
                    Doctor = doctor,
                    TodayAppointments = await _context.Appointments
                        .Include(a => a.Patient)
                        .Where(a => a.DoctorId == doctor.DoctorId &&
                                   a.AppointmentDate.Date == DateTime.Today &&
                                   a.Status != "Cancelled")
                        .OrderBy(a => a.AppointmentTime)
                        .ToListAsync(),
                    UpcomingAppointments = await _context.Appointments
                        .Include(a => a.Patient)
                        .Where(a => a.DoctorId == doctor.DoctorId &&
                                   a.AppointmentDate >= DateTime.Today &&
                                   a.Status == "Pending")
                        .OrderBy(a => a.AppointmentDate)
                        .ThenBy(a => a.AppointmentTime)
                        .Take(10)
                        .ToListAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard";
                return View(new DoctorDashboardViewModel());
            }
        }

        // LABEL: Appointments List
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogWarning("Doctor profile not found for user: {UserId}", user.Id);
                    return NotFound();
                }

                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                    .Where(a => a.DoctorId == doctor.DoctorId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.AppointmentTime)
                    .ToListAsync();

                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor appointments");
                TempData["ErrorMessage"] = "Error loading appointments";
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
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogError("Doctor not found for status update");
                    TempData["ErrorMessage"] = "Doctor not found!";
                    return RedirectToAction("Appointments");
                }

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for status update", appointmentId);
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("Appointments");
                }

                appointment.Status = status;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} status updated to {Status}", appointmentId, status);
                TempData["SuccessMessage"] = $"Appointment status updated to {status} successfully!";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment {AppointmentId} status", appointmentId);
                TempData["ErrorMessage"] = $"Error updating appointment: {ex.Message}";
                return RedirectToAction("Appointments");
            }
        }

        // LABEL: Complete Appointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int appointmentId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogError("Doctor not found for completing appointment");
                    TempData["ErrorMessage"] = "Doctor not found!";
                    return RedirectToAction("Appointments");
                }

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for completion", appointmentId);
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("Appointments");
                }

                appointment.Status = "Completed";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} marked as completed", appointmentId);
                TempData["SuccessMessage"] = "Appointment marked as completed!";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment {AppointmentId}", appointmentId);
                TempData["ErrorMessage"] = $"Error completing appointment: {ex.Message}";
                return RedirectToAction("Appointments");
            }
        }

        // LABEL: Patient History
        [HttpGet]
        public async Task<IActionResult> PatientHistory(int patientId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogWarning("Doctor profile not found for user: {UserId}", user.Id);
                    return NotFound();
                }

                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId);

                if (patient == null)
                {
                    _logger.LogWarning("Patient {PatientId} not found", patientId);
                    TempData["ErrorMessage"] = "Patient not found!";
                    return RedirectToAction("Appointments");
                }

                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == patientId && a.DoctorId == doctor.DoctorId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.AppointmentTime)
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
                _logger.LogError(ex, "Error loading patient history for patient {PatientId}", patientId);
                TempData["ErrorMessage"] = "Error loading patient history";
                return RedirectToAction("Appointments");
            }
        }

        // LABEL: Add Medical Notes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedicalNotes(MedicalNotesModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogError("Doctor not found for adding medical notes");
                    return Json(new { success = false, message = "Doctor not found" });
                }

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId && a.DoctorId == doctor.DoctorId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for adding notes", model.AppointmentId);
                    return Json(new { success = false, message = "Appointment not found" });
                }

                appointment.Notes = model.Notes;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Medical notes added to appointment {AppointmentId}", model.AppointmentId);
                return Json(new { success = true, message = "Medical notes added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medical notes to appointment {AppointmentId}", model.AppointmentId);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // LABEL: Update Appointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointment(UpdateAppointmentModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogError("Doctor not found for updating appointment");
                    return Json(new { success = false, message = "Doctor not found" });
                }

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId && a.DoctorId == doctor.DoctorId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for update", model.AppointmentId);
                    return Json(new { success = false, message = "Appointment not found" });
                }

                appointment.Status = model.Status;
                if (!string.IsNullOrEmpty(model.Notes))
                {
                    appointment.Notes = model.Notes;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} updated successfully", model.AppointmentId);
                return Json(new { success = true, message = "Appointment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment {AppointmentId}", model.AppointmentId);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // LABEL: Get Today's Appointments
        [HttpGet]
        public async Task<IActionResult> GetTodayAppointments()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogError("Doctor not found for getting today's appointments");
                    return Json(new { success = false, message = "Doctor not found" });
                }

                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.DoctorId &&
                               a.AppointmentDate.Date == DateTime.Today &&
                               a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentTime)
                    .Select(a => new
                    {
                        a.AppointmentId,
                        a.AppointmentTime,
                        PatientName = a.Patient.Name,
                        a.Status,
                        a.Notes
                    })
                    .ToListAsync();

                return Json(new { success = true, appointments = appointments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's appointments");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // LABEL: Send Appointment Reminder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAppointmentReminder(int appointmentId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);

                if (doctor == null)
                {
                    _logger.LogError("Doctor not found for sending reminder");
                    return Json(new { success = false, message = "Doctor not found" });
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for reminder", appointmentId);
                    return Json(new { success = false, message = "Appointment not found" });
                }

                // Simulate sending reminder (integrate with email/SMS service in production)
                _logger.LogInformation("Reminder sent for appointment {AppointmentId} with patient {PatientName}",
                    appointmentId, appointment.Patient.Name);

                return Json(new
                {
                    success = true,
                    message = $"Reminder sent for appointment with {appointment.Patient.Name} on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.AppointmentTime:hh\\:mm}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder for appointment {AppointmentId}", appointmentId);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}