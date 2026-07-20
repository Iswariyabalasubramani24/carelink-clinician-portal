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
    }
}
