using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ReportSettingsRepository(ApplicationDbContext db) : IReportSettingsRepository
{
    public async Task<ReportSettings?> GetByTenantIdAsync(int tenantId)
    {
        return await db.ReportSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);
    }

    public async Task<ReportSettings?> GetByPatientIdAsync(int patientId)
    {
        return await db.ReportSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PatientId == patientId);
    }

    public async Task UpsertTenantSettingsAsync(int tenantId, int intervalDays)
    {
        var existing = await db.ReportSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (existing is null)
        {
            db.ReportSettings.Add(new ReportSettings
            {
                TenantId = tenantId,
                IntervalDays = intervalDays
            });
        }
        else
        {
            existing.IntervalDays = intervalDays;
        }

        await db.SaveChangesAsync();
    }

    public async Task UpsertPatientOverrideAsync(int patientId, int intervalDays)
    {
        var existing = await db.ReportSettings
            .FirstOrDefaultAsync(s => s.PatientId == patientId);

        if (existing is null)
        {
            db.ReportSettings.Add(new ReportSettings
            {
                PatientId = patientId,
                IntervalDays = intervalDays
            });
        }
        else
        {
            existing.IntervalDays = intervalDays;
        }

        await db.SaveChangesAsync();
    }

    public async Task RemovePatientOverrideAsync(int patientId)
    {
        var existing = await db.ReportSettings
            .FirstOrDefaultAsync(s => s.PatientId == patientId);

        if (existing is not null)
        {
            db.ReportSettings.Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}
