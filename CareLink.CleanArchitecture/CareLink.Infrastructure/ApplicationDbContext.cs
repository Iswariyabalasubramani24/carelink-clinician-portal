using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Clinician> Clinicians => Set<Clinician>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ClinicianTenant> ClinicianTenants => Set<ClinicianTenant>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<ClinicAlertSettings> ClinicAlertSettings => Set<ClinicAlertSettings>();

    public DbSet<PatientAlertSettings> PatientAlertSettings => Set<PatientAlertSettings>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ReportSettings> ReportSettings => Set<ReportSettings>();

    public DbSet<PatientNote> PatientNotes => Set<PatientNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Region).IsRequired().HasMaxLength(100);
            entity.Property(t => t.LanguageCode).IsRequired().HasMaxLength(10);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.MedicalRecordNumber).IsRequired().HasMaxLength(20);
            entity.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.LastName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.DeviceSerialNumber).IsRequired().HasMaxLength(100);
            entity.Property(p => p.DeviceType).HasConversion<string>();
            entity.Property(p => p.BatteryLevel).HasPrecision(5, 2);

            entity.HasOne(p => p.Tenant)
                .WithMany(t => t.Patients)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Clinician>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.PasswordHash).IsRequired();
            entity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Role).HasConversion<string>();

            entity.HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).IsRequired().HasMaxLength(512);
            entity.HasIndex(r => r.Token).IsUnique();

            entity.HasOne(r => r.Clinician)
                .WithMany()
                .HasForeignKey(r => r.ClinicianId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicianTenant>(entity =>
        {
            entity.HasKey(ct => ct.Id);
            entity.HasIndex(ct => new { ct.ClinicianId, ct.TenantId }).IsUnique();

            entity.HasOne(ct => ct.Clinician)
                .WithMany()
                .HasForeignKey(ct => ct.ClinicianId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ct => ct.Tenant)
                .WithMany()
                .HasForeignKey(ct => ct.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AlertType).HasConversion<string>();
            entity.Property(a => a.Urgency).HasConversion<string>();
            entity.HasIndex(a => new { a.PatientId, a.AlertType });

            entity.HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicAlertSettings>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.AlertType).HasConversion<string>();
            entity.Property(s => s.DefaultUrgency).HasConversion<string>();
            entity.HasIndex(s => new { s.TenantId, s.AlertType }).IsUnique();

            entity.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PatientAlertSettings>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.AlertType).HasConversion<string>();
            entity.Property(s => s.Urgency).HasConversion<string>();
            entity.HasIndex(s => new { s.PatientId, s.AlertType }).IsUnique();

            entity.HasOne(s => s.Patient)
                .WithMany()
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ReportType).HasConversion<string>();
            entity.Property(r => r.DataSnapshot).IsRequired();
            entity.HasIndex(r => r.PatientId);

            entity.HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportSettings>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TenantId).IsUnique().HasFilter("[TenantId] IS NOT NULL");
            entity.HasIndex(s => s.PatientId).IsUnique().HasFilter("[PatientId] IS NOT NULL");

            entity.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Patient)
                .WithMany()
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PatientNote>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Content).IsRequired();
            entity.HasIndex(n => n.PatientId);

            entity.HasOne(n => n.Patient)
                .WithMany()
                .HasForeignKey(n => n.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(n => n.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Clinician>()
                .WithMany()
                .HasForeignKey(n => n.ClinicianId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
