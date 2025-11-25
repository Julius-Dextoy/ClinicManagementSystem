using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Models
{
    public static class SeedData
    {
        // LABEL: Initialize Seed Data
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Define roles with correct capitalization
                string[] roleNames = { "Admin", "Doctor", "Patient" };

                // LABEL: Create Roles
                // Create roles if they don't exist
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        // Create the role
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // LABEL: Create Admin User
                // Create default admin user
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@clinic.com",
                    Email = "admin@clinic.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                string adminPassword = "Admin123!";
                var existingAdmin = await userManager.FindByEmailAsync(adminUser.Email);

                if (existingAdmin == null)
                {
                    var createPowerUser = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createPowerUser.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // LABEL: Create Sample Doctor
                // Create a sample doctor for testing
                var doctorUser = new ApplicationUser
                {
                    UserName = "doctor@clinic.com",
                    Email = "doctor@clinic.com",
                    FullName = "Dr. Dexter Tenchavez",
                    EmailConfirmed = true
                };

                string doctorPassword = "Doctor123!";
                var existingDoctor = await userManager.FindByEmailAsync(doctorUser.Email);

                if (existingDoctor == null)
                {
                    var createDoctor = await userManager.CreateAsync(doctorUser, doctorPassword);
                    if (createDoctor.Succeeded)
                    {
                        await userManager.AddToRoleAsync(doctorUser, "Doctor");

                        // Create doctor profile
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var doctor = new Doctor
                        {
                            UserId = doctorUser.Id,
                            Name = "Dr. Vincent Tyros",
                            Specialization = "General Medicine",
                            Phone = "+1234567890",
                            Address = "Gabi Ubay, Bohol",
                            IsActive = true
                        };
                        context.Doctors.Add(doctor);
                        await context.SaveChangesAsync();
                    }
                }

                // LABEL: Create Sample Patient
                // Create a sample patient for testing
                var patientUser = new ApplicationUser
                {
                    UserName = "julius@clinic.com",
                    Email = "julius@clinic.com",
                    FullName = "Julius",
                    EmailConfirmed = true
                };

                string patientPassword = "Patient123!";
                var existingPatient = await userManager.FindByEmailAsync(patientUser.Email);

                if (existingPatient == null)
                {
                    var createPatient = await userManager.CreateAsync(patientUser, patientPassword);
                    if (createPatient.Succeeded)
                    {
                        await userManager.AddToRoleAsync(patientUser, "Patient");

                        // Create patient profile
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var patient = new Patient
                        {
                            UserId = patientUser.Id,
                            Name = "Felicity Valecera",
                            Age = 21,
                            Phone = "+0987654321",
                            Address = "Lahacienda Alicia, Bohol",
                            MedicalHistory = "No significant medical history",
                            RegistrationDate = DateTime.Now
                        };
                        context.Patients.Add(patient);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}