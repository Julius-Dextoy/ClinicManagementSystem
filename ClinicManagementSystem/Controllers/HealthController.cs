using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using System;
using System.Threading.Tasks;

namespace ClinicManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Check database connectivity
                var canConnect = await _context.Database.CanConnectAsync();

                // Check if we can execute a simple query
                var doctorCount = await _context.Doctors.CountAsync();
                var patientCount = await _context.Patients.CountAsync();
                var appointmentCount = await _context.Appointments.CountAsync();

                return Ok(new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    database = "Connected",
                    statistics = new
                    {
                        doctors = doctorCount,
                        patients = patientCount,
                        appointments = appointmentCount
                    },
                    checks = new
                    {
                        database = "OK",
                        api = "OK"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    timestamp = DateTime.UtcNow,
                    database = "Disconnected",
                    error = ex.Message,
                    checks = new
                    {
                        database = "FAILED",
                        api = "OK"
                    }
                });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "Pong",
                timestamp = DateTime.UtcNow,
                server = Environment.MachineName
            });
        }
    }
}