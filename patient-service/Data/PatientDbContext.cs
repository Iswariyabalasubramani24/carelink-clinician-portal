using CareLink.PatientService.Models;
using Microsoft.EntityFrameworkCore;

namespace CareLink.PatientService.Data;

public class PatientDbContext(DbContextOptions<PatientDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(p => p.MedicalRecordNumber).IsUnique();
            entity.HasIndex(p => p.DeviceSerialNumber).IsUnique();
            entity.Property(p => p.DeviceType).HasConversion<string>();
        });
    }
}
