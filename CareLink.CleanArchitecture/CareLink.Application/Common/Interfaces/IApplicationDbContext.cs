using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<Patient> Patients { get; }

    DbSet<Clinician> Clinicians { get; }

    DbSet<ClinicianTenant> ClinicianTenants { get; }

    DbSet<ClinicAlertSettings> ClinicAlertSettings { get; }

    DbSet<PatientScheduleSettings> PatientScheduleSettings { get; }

    DbSet<ReportSettings> ReportSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
