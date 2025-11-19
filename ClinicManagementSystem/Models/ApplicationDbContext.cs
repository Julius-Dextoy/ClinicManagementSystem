using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using System;

namespace ClinicManagementSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity tables first
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName).HasMaxLength(100);
                entity.Property(u => u.CreatedAt).IsRequired();
            });

            // Patient configuration
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.PatientId);

                // Make UserId optional for initial creation
                entity.Property(p => p.UserId).IsRequired(false);

                entity.HasOne(p => p.User)
                      .WithOne(u => u.Patient)
                      .HasForeignKey<Patient>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Age).IsRequired();
                entity.Property(p => p.Phone).HasMaxLength(15).IsRequired(false);
                entity.Property(p => p.Address).HasMaxLength(200).HasDefaultValue("Not provided");
                entity.Property(p => p.MedicalHistory).HasMaxLength(1000).HasDefaultValue("No medical history provided");
                entity.Property(p => p.RegistrationDate).IsRequired();
            });

            // Doctor configuration
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasKey(e => e.DoctorId);

                // Make UserId optional for initial creation
                entity.Property(d => d.UserId).IsRequired(false);

                entity.HasOne(d => d.User)
                      .WithOne(u => u.Doctor)
                      .HasForeignKey<Doctor>(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Specialization).IsRequired().HasMaxLength(50);
                entity.Property(d => d.Phone).IsRequired().HasMaxLength(15);
                entity.Property(d => d.Address).HasMaxLength(200).HasDefaultValue("Not provided");
                entity.Property(d => d.IsActive).HasDefaultValue(true);
            });

            // Appointment configuration
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(e => e.AppointmentId);

                entity.HasOne(a => a.Doctor)
                      .WithMany(d => d.Appointments)
                      .HasForeignKey(a => a.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Patient)
                      .WithMany(p => p.Appointments)
                      .HasForeignKey(a => a.PatientId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(a => a.Notes).HasMaxLength(500).IsRequired(false);
                entity.Property(a => a.CreatedAt).IsRequired();

                // Index for better query performance
                entity.HasIndex(a => new { a.AppointmentDate, a.AppointmentTime });
                entity.HasIndex(a => a.Status);
            });

            // REMOVED: Role seeding - we'll handle this separately
            // The issue is that roles are inserted before unique indexes are created
        }
    }
}