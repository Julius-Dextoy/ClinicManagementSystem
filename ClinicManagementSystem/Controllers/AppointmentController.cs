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
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AppointmentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Book()
        {
            try
            {
                _logger.LogInformation("Loading booking page for user: {User}", User.Identity?.Name);

                var doctors = await _context.Doctors
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active doctors", doctors.Count);

                var model = new BookAppointmentViewModel
                {
                    Doctors = doctors,
                    AvailableTimeSlots = GetTimeSlots(),
                    AppointmentDate = DateTime.Today
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking page");
                TempData["ErrorMessage"] = $"Error loading booking page: {ex.Message}";

                // Return properly initialized model even on error
                return View(new BookAppointmentViewModel
                {
                    Doctors = new List<Doctor>(),
                    AvailableTimeSlots = GetTimeSlots()
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentViewModel model)
        {
            try
            {
                // ALWAYS populate Doctors and AvailableTimeSlots FIRST
                model.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
                model.AvailableTimeSlots = GetTimeSlots();

                _logger.LogInformation("=== BOOK APPOINTMENT PROCESS STARTED ===");
                _logger.LogInformation("User: {User}", User.Identity?.Name);
                _logger.LogInformation("Model Data - DoctorId: {DoctorId}, Date: {Date}, Time: {Time}",
                    model.DoctorId, model.AppointmentDate, model.AppointmentTime);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            _logger.LogWarning("Validation Error - Field: {Field}, Error: {Error}",
                                state.Key, error.ErrorMessage);
                        }
                    }
                    return View(model);
                }

                // Use the execution strategy to handle transactions
                var executionStrategy = _context.Database.CreateExecutionStrategy();
                var successMessage = "";

                await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        _logger.LogInformation("STEP 1: Getting current user");
                        var user = await _userManager.GetUserAsync(User);
                        if (user == null)
                        {
                            _logger.LogError("User not found in database");
                            throw new Exception("User not found! Please log in again.");
                        }

                        // Get or create patient profile
                        _logger.LogInformation("STEP 2: Finding or creating patient profile");
                        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);

                        if (patient == null)
                        {
                            _logger.LogInformation("Creating new patient profile for user: {UserId}", user.Id);
                            patient = new Patient
                            {
                                UserId = user.Id,
                                Name = user.FullName ?? user.UserName ?? "Unknown Patient",
                                Age = 0,
                                Phone = user.PhoneNumber ?? "",
                                Address = "Not provided",
                                MedicalHistory = "No medical history provided",
                                RegistrationDate = DateTime.Now
                            };
                            _context.Patients.Add(patient);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Patient created successfully - ID: {PatientId}", patient.PatientId);
                        }
                        else
                        {
                            _logger.LogInformation("Patient profile exists - ID: {PatientId}", patient.PatientId);
                        }

                        // Validate the doctor exists and is active
                        _logger.LogInformation("STEP 3: Validating doctor ID: {DoctorId}", model.DoctorId);
                        var doctor = await _context.Doctors
                            .FirstOrDefaultAsync(d => d.DoctorId == model.DoctorId && d.IsActive);

                        if (doctor == null)
                        {
                            _logger.LogError("Doctor not found or inactive - DoctorId: {DoctorId}", model.DoctorId);
                            throw new Exception("Selected doctor is not available. Please choose a different doctor.");
                        }
                        _logger.LogInformation("Doctor validated - Name: {DoctorName}", doctor.Name);

                        // Validate appointment date
                        _logger.LogInformation("STEP 4: Validating appointment date: {AppointmentDate}", model.AppointmentDate);
                        if (model.AppointmentDate < DateTime.Today)
                        {
                            _logger.LogWarning("Appointment date is in the past: {AppointmentDate}", model.AppointmentDate);
                            throw new Exception("Appointment date cannot be in the past.");
                        }

                        // Check for time slot availability
                        _logger.LogInformation("STEP 5: Checking time slot availability");
                        var existingAppointment = await _context.Appointments
                            .Where(a => a.DoctorId == model.DoctorId &&
                                       a.AppointmentDate == model.AppointmentDate &&
                                       a.AppointmentTime == model.AppointmentTime &&
                                       (a.Status == "Pending" || a.Status == "Approved" || a.Status == "Confirmed"))
                            .FirstOrDefaultAsync();

                        if (existingAppointment != null)
                        {
                            _logger.LogWarning("Time slot already booked - Doctor: {DoctorId}, Date: {Date}, Time: {Time}",
                                model.DoctorId, model.AppointmentDate, model.AppointmentTime);
                            throw new Exception("This time slot is already booked. Please choose a different time.");
                        }
                        _logger.LogInformation("Time slot is available");

                        // Create the appointment
                        _logger.LogInformation("STEP 6: Creating appointment object");
                        var appointment = new Appointment
                        {
                            DoctorId = model.DoctorId,
                            PatientId = patient.PatientId,
                            AppointmentDate = model.AppointmentDate,
                            AppointmentTime = model.AppointmentTime,
                            Status = "Pending",
                            Notes = model.Notes?.Trim(),
                            CreatedAt = DateTime.Now
                        };

                        _logger.LogInformation("Appointment details - DoctorId: {DoctorId}, PatientId: {PatientId}, Date: {Date}, Time: {Time}",
                            appointment.DoctorId, appointment.PatientId, appointment.AppointmentDate, appointment.AppointmentTime);

                        _context.Appointments.Add(appointment);
                        var saveResult = await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation("Appointment booked successfully - ID: {AppointmentId}, Rows affected: {Rows}",
                            appointment.AppointmentId, saveResult);

                        successMessage = $"Appointment booked successfully! Your appointment ID is #{appointment.AppointmentId}. It is now pending approval.";
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction("MyAppointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Book POST method");
                TempData["ErrorMessage"] = $"An error occurred while booking the appointment: {ex.Message}";

                // Ensure model is populated even in outer catch
                model.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
                model.AvailableTimeSlots = GetTimeSlots();
                return View(model);
            }
        }

        public async Task<IActionResult> MyAppointments()
        {
            try
            {
                _logger.LogInformation("Loading appointments for user: {User}", User.Identity?.Name);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found when loading appointments");
                    TempData["ErrorMessage"] = "User not found!";
                    return View(new List<Appointment>());
                }

                List<Appointment> appointments = new List<Appointment>();

                if (User.IsInRole("Patient"))
                {
                    _logger.LogInformation("Loading appointments for patient");
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (patient == null)
                    {
                        _logger.LogInformation("No patient profile found for user: {UserId}", user.Id);
                        TempData["InfoMessage"] = "You don't have any appointments yet. Book your first appointment!";
                        return View(appointments);
                    }

                    appointments = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Where(a => a.PatientId == patient.PatientId)
                        .OrderByDescending(a => a.AppointmentDate)
                        .ThenBy(a => a.AppointmentTime)
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} appointments for patient {PatientId}",
                        appointments.Count, patient.PatientId);
                }
                else if (User.IsInRole("Doctor"))
                {
                    _logger.LogInformation("Loading appointments for doctor");
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (doctor == null)
                    {
                        TempData["ErrorMessage"] = "Doctor profile not found!";
                        return View(appointments);
                    }

                    appointments = await _context.Appointments
                        .Include(a => a.Patient)
                        .Where(a => a.DoctorId == doctor.DoctorId)
                        .OrderByDescending(a => a.AppointmentDate)
                        .ThenBy(a => a.AppointmentTime)
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} appointments for doctor {DoctorId}",
                        appointments.Count, doctor.DoctorId);
                }
                else if (User.IsInRole("Admin"))
                {
                    _logger.LogInformation("Loading all appointments for admin");
                    appointments = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .OrderByDescending(a => a.AppointmentDate)
                        .ThenBy(a => a.AppointmentTime)
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} total appointments", appointments.Count);
                }

                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointments");
                TempData["ErrorMessage"] = $"Error loading appointments: {ex.Message}";
                return View(new List<Appointment>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllAppointments()
        {
            try
            {
                _logger.LogInformation("Loading all appointments for admin");

                var appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} total appointments", appointments.Count);
                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all appointments");
                TempData["ErrorMessage"] = $"Error loading appointments: {ex.Message}";
                return View(new List<Appointment>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            try
            {
                _logger.LogInformation("Cancelling appointment {AppointmentId} for user: {User}", id, User.Identity?.Name);

                var user = await _userManager.GetUserAsync(User);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (patient == null)
                {
                    _logger.LogWarning("Patient profile not found for user: {UserId}", user.Id);
                    TempData["ErrorMessage"] = "Patient profile not found!";
                    return RedirectToAction("MyAppointments");
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id && a.PatientId == patient.PatientId);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment {AppointmentId} not found for patient {PatientId}", id, patient.PatientId);
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("MyAppointments");
                }

                if (appointment.Status == "Completed")
                {
                    _logger.LogWarning("Cannot cancel completed appointment {AppointmentId}", id);
                    TempData["ErrorMessage"] = "Cannot cancel completed appointments.";
                    return RedirectToAction("MyAppointments");
                }

                if (appointment.AppointmentDate < DateTime.Today)
                {
                    _logger.LogWarning("Cannot cancel past appointment {AppointmentId}", id);
                    TempData["ErrorMessage"] = "Cannot cancel past appointments.";
                    return RedirectToAction("MyAppointments");
                }

                if (appointment.Status == "Cancelled")
                {
                    _logger.LogWarning("Appointment {AppointmentId} is already cancelled", id);
                    TempData["WarningMessage"] = "Appointment is already cancelled.";
                    return RedirectToAction("MyAppointments");
                }

                appointment.Status = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} cancelled successfully", id);
                TempData["SuccessMessage"] = $"Appointment with Dr. {appointment.Doctor?.Name} on {appointment.AppointmentDate:MMM dd, yyyy} has been cancelled successfully!";
                return RedirectToAction("MyAppointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
                TempData["ErrorMessage"] = $"Error cancelling appointment: {ex.Message}";
                return RedirectToAction("MyAppointments");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                _logger.LogInformation("Admin deleting appointment {AppointmentId}", id);

                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment {AppointmentId} not found for deletion", id);
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("AllAppointments");
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} deleted successfully by admin", id);
                TempData["SuccessMessage"] = $"Appointment #{appointment.AppointmentId} has been deleted successfully!";
                return RedirectToAction("AllAppointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting appointment {AppointmentId}", id);
                TempData["ErrorMessage"] = $"Error deleting appointment: {ex.Message}";
                return RedirectToAction("AllAppointments");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteAppointments(List<int> appointmentIds)
        {
            try
            {
                if (appointmentIds == null || !appointmentIds.Any())
                {
                    TempData["ErrorMessage"] = "No appointments selected for deletion!";
                    return RedirectToAction("AllAppointments");
                }

                _logger.LogInformation("Admin bulk deleting {Count} appointments", appointmentIds.Count);

                var appointments = await _context.Appointments
                    .Where(a => appointmentIds.Contains(a.AppointmentId))
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .ToListAsync();

                if (!appointments.Any())
                {
                    TempData["ErrorMessage"] = "No valid appointments found for deletion!";
                    return RedirectToAction("AllAppointments");
                }

                _context.Appointments.RemoveRange(appointments);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted {Count} appointments", appointments.Count);
                TempData["SuccessMessage"] = $"Successfully deleted {appointments.Count} appointment(s)!";
                return RedirectToAction("AllAppointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk appointment deletion");
                TempData["ErrorMessage"] = $"Error deleting appointments: {ex.Message}";
                return RedirectToAction("AllAppointments");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AppointmentDetails(int id)
        {
            try
            {
                _logger.LogInformation("Loading appointment details for ID: {AppointmentId}, User: {User}", id, User.Identity?.Name);

                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment {AppointmentId} not found", id);
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("MyAppointments");
                }

                // Check if user has permission to view this appointment
                var user = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Patient"))
                {
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (patient == null || appointment.PatientId != patient.PatientId)
                    {
                        _logger.LogWarning("Access denied - Patient {PatientId} cannot view appointment {AppointmentId}",
                            patient?.PatientId, id);
                        TempData["ErrorMessage"] = "Access denied!";
                        return RedirectToAction("MyAppointments");
                    }
                }
                else if (User.IsInRole("Doctor"))
                {
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (doctor == null || appointment.DoctorId != doctor.DoctorId)
                    {
                        _logger.LogWarning("Access denied - Doctor {DoctorId} cannot view appointment {AppointmentId}",
                            doctor?.DoctorId, id);
                        TempData["ErrorMessage"] = "Access denied!";
                        return RedirectToAction("MyAppointments");
                    }
                }

                _logger.LogInformation("Appointment details loaded successfully for ID: {AppointmentId}", id);
                return View(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointment details for ID: {AppointmentId}", id);
                TempData["ErrorMessage"] = $"Error loading appointment details: {ex.Message}";
                return RedirectToAction("MyAppointments");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId, DateTime newDate, TimeSpan newTime)
        {
            try
            {
                _logger.LogInformation("Rescheduling appointment {AppointmentId} to {NewDate} {NewTime}",
                    appointmentId, newDate, newTime);

                var user = await _userManager.GetUserAsync(User);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);

                if (patient == null)
                {
                    _logger.LogError("Patient not found for rescheduling");
                    return Json(new { success = false, message = "Patient not found" });
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patient.PatientId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for rescheduling", appointmentId);
                    return Json(new { success = false, message = "Appointment not found" });
                }

                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    _logger.LogWarning("Cannot reschedule {Status} appointment {AppointmentId}", appointment.Status, appointmentId);
                    return Json(new { success = false, message = $"Cannot reschedule a {appointment.Status.ToLower()} appointment" });
                }

                if (newDate < DateTime.Today)
                {
                    _logger.LogWarning("Cannot reschedule to past date: {NewDate}", newDate);
                    return Json(new { success = false, message = "Cannot reschedule to a past date" });
                }

                // Check if new time slot is available
                var existingAppointment = await _context.Appointments
                    .Where(a => a.DoctorId == appointment.DoctorId &&
                               a.AppointmentDate == newDate &&
                               a.AppointmentTime == newTime &&
                               a.Status != "Cancelled" &&
                               a.AppointmentId != appointmentId)
                    .FirstOrDefaultAsync();

                if (existingAppointment != null)
                {
                    _logger.LogWarning("Time slot already booked for rescheduling");
                    return Json(new { success = false, message = "This time slot is already booked. Please choose a different time." });
                }

                appointment.AppointmentDate = newDate;
                appointment.AppointmentTime = newTime;
                appointment.Status = "Pending"; // Reset to pending for approval
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} rescheduled successfully", appointmentId);
                return Json(new { success = true, message = "Appointment rescheduled successfully! Waiting for doctor approval." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling appointment {AppointmentId}", appointmentId);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, string status)
        {
            try
            {
                _logger.LogInformation("Updating appointment {AppointmentId} status to {Status}", appointmentId, status);

                // Validate status
                var validStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, message = "Invalid status provided." });
                }

                var user = await _userManager.GetUserAsync(User);
                Appointment appointment;

                if (User.IsInRole("Admin"))
                {
                    // Admin can update any appointment
                    appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
                }
                else
                {
                    // Doctor can only update their own appointments
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (doctor == null)
                    {
                        _logger.LogError("Doctor not found for status update");
                        return Json(new { success = false, message = "Doctor not found" });
                    }

                    appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId);
                }

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for status update", appointmentId);
                    return Json(new { success = false, message = "Appointment not found" });
                }

                var oldStatus = appointment.Status;
                appointment.Status = status;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} status updated from {OldStatus} to {NewStatus}",
                    appointmentId, oldStatus, status);

                return Json(new
                {
                    success = true,
                    message = $"Appointment status updated to {status.ToLower()} successfully!",
                    newStatus = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment {AppointmentId} status", appointmentId);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int appointmentId)
        {
            try
            {
                _logger.LogInformation("Completing appointment {AppointmentId}", appointmentId);

                var user = await _userManager.GetUserAsync(User);
                Appointment appointment;

                if (User.IsInRole("Admin"))
                {
                    appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
                }
                else
                {
                    var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                    if (doctor == null)
                    {
                        _logger.LogError("Doctor not found for completing appointment");
                        TempData["ErrorMessage"] = "Doctor not found!";
                        return RedirectToAction("MyAppointments");
                    }

                    appointment = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctor.DoctorId);
                }

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for completion", appointmentId);
                    TempData["ErrorMessage"] = "Appointment not found!";
                    return RedirectToAction("MyAppointments");
                }

                appointment.Status = "Completed";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appointment {AppointmentId} marked as completed", appointmentId);
                TempData["SuccessMessage"] = $"Appointment with {appointment.Patient?.Name} has been marked as completed!";
                return RedirectToAction("AppointmentDetails", new { id = appointmentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing appointment {AppointmentId}", appointmentId);
                TempData["ErrorMessage"] = $"Error completing appointment: {ex.Message}";
                return RedirectToAction("MyAppointments");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int doctorId, DateTime date)
        {
            try
            {
                _logger.LogInformation("Getting available time slots for doctor {DoctorId} on {Date}", doctorId, date);

                if (date < DateTime.Today)
                {
                    _logger.LogWarning("Cannot get time slots for past date: {Date}", date);
                    return Json(new { success = false, message = "Cannot book appointments in the past" });
                }

                // Check if doctor exists and is active
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.IsActive);
                if (doctor == null)
                {
                    return Json(new { success = false, message = "Doctor not found or inactive" });
                }

                var bookedSlots = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId &&
                               a.AppointmentDate == date &&
                               (a.Status == "Pending" || a.Status == "Confirmed" || a.Status == "Approved"))
                    .Select(a => a.AppointmentTime)
                    .ToListAsync();

                var allSlots = GetTimeSlots();
                var availableSlots = allSlots.Where(slot => !bookedSlots.Contains(slot)).ToList();

                _logger.LogInformation("Found {AvailableCount} available slots out of {TotalCount} total slots for doctor {DoctorId} on {Date}",
                    availableSlots.Count, allSlots.Count, doctorId, date);

                return Json(new
                {
                    success = true,
                    slots = availableSlots,
                    doctorName = doctor.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available time slots for doctor {DoctorId}", doctorId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> SendAppointmentReminder(int appointmentId)
        {
            try
            {
                _logger.LogInformation("Sending reminder for appointment {AppointmentId}", appointmentId);

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    _logger.LogError("Appointment {AppointmentId} not found for reminder", appointmentId);
                    return Json(new { success = false, message = "Appointment not found" });
                }

                // Simulate sending reminder (in real app, integrate with email/SMS service)
                _logger.LogInformation("Reminder simulation for appointment {AppointmentId}", appointmentId);

                // You would typically call your email service here
                // await _emailService.SendAppointmentReminder(appointment);

                _logger.LogInformation("Reminder sent for appointment {AppointmentId}", appointmentId);
                return Json(new
                {
                    success = true,
                    message = $"Reminder sent for appointment with Dr. {appointment.Doctor.Name} on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.AppointmentTime:hh\\:mm}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder for appointment {AppointmentId}", appointmentId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        public async Task<IActionResult> DebugInfo()
        {
            try
            {
                _logger.LogInformation("Loading debug info for user: {User}", User.Identity?.Name);

                var user = await _userManager.GetUserAsync(User);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                var doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
                var appointments = patient != null ? await _context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == patient.PatientId)
                    .ToListAsync() : new List<Appointment>();

                ViewBag.User = user;
                ViewBag.Patient = patient;
                ViewBag.Doctors = doctors;
                ViewBag.Appointments = appointments;
                ViewBag.DoctorsCount = doctors.Count;
                ViewBag.AppointmentsCount = appointments.Count;

                _logger.LogInformation("Debug info loaded - Doctors: {DoctorsCount}, Appointments: {AppointmentsCount}",
                    doctors.Count, appointments.Count);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading debug info");
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> TestBooking()
        {
            try
            {
                _logger.LogInformation("Starting test booking for user: {User}", User.Identity?.Name);

                var user = await _userManager.GetUserAsync(User);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                var doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
                var firstDoctor = doctors.FirstOrDefault();

                if (firstDoctor != null && patient != null)
                {
                    // Find an available time slot tomorrow
                    var tomorrow = DateTime.Today.AddDays(1);
                    var availableSlots = await GetAvailableTimeSlotsForDoctor(firstDoctor.DoctorId, tomorrow);
                    var firstAvailableSlot = availableSlots.FirstOrDefault();

                    if (firstAvailableSlot != default(TimeSpan))
                    {
                        var testAppointment = new Appointment
                        {
                            DoctorId = firstDoctor.DoctorId,
                            PatientId = patient.PatientId,
                            AppointmentDate = tomorrow,
                            AppointmentTime = firstAvailableSlot,
                            Status = "Pending",
                            Notes = "Test appointment from debug functionality",
                            CreatedAt = DateTime.Now
                        };

                        _context.Appointments.Add(testAppointment);
                        var result = await _context.SaveChangesAsync();

                        _logger.LogInformation("Test appointment created successfully - ID: {AppointmentId}, Rows: {Rows}",
                            testAppointment.AppointmentId, result);

                        TempData["SuccessMessage"] = $"Test appointment created successfully! Appointment ID: #{testAppointment.AppointmentId}, Doctor: Dr. {firstDoctor.Name}, Date: {tomorrow:MMM dd, yyyy}, Time: {firstAvailableSlot:hh\\:mm}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No available time slots found for test booking.";
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot create test appointment - Doctor: {HasDoctor}, Patient: {HasPatient}",
                        firstDoctor != null, patient != null);
                    TempData["ErrorMessage"] = $"Cannot create test appointment. Doctor available: {firstDoctor != null}, Patient profile: {patient != null}";
                }

                return RedirectToAction("MyAppointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test booking failed");
                TempData["ErrorMessage"] = $"Test booking failed: {ex.Message}";
                return RedirectToAction("Book");
            }
        }

        private async Task<List<TimeSpan>> GetAvailableTimeSlotsForDoctor(int doctorId, DateTime date)
        {
            var bookedSlots = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate == date &&
                           (a.Status == "Pending" || a.Status == "Confirmed" || a.Status == "Approved"))
                .Select(a => a.AppointmentTime)
                .ToListAsync();

            var allSlots = GetTimeSlots();
            return allSlots.Where(slot => !bookedSlots.Contains(slot)).ToList();
        }

        private List<TimeSpan> GetTimeSlots()
        {
            var timeSlots = new List<TimeSpan>();
            var startTime = new TimeSpan(9, 0, 0); // 9:00 AM
            var endTime = new TimeSpan(17, 0, 0);  // 5:00 PM

            for (var time = startTime; time <= endTime; time = time.Add(new TimeSpan(0, 30, 0)))
            {
                timeSlots.Add(time);
            }

            return timeSlots;
        }
    }
}