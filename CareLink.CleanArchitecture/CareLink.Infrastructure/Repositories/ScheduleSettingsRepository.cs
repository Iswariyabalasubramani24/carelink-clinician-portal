using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ScheduleSettingsRepository(ApplicationDbContext db) : IScheduleSettingsRepository
{
    public async Task<PatientScheduleSettings?> GetByTenantIdAsync(int tenantId)
    {
        return await db.PatientScheduleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);
    }

    public async Task<PatientScheduleSettings?> GetByPatientIdAsync(int patientId)
    {
        return await db.PatientScheduleSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PatientId == patientId);
    }

    public async Task UpsertTenantSettingsAsync(int tenantId, int intervalDays)
    {
        var existing = await db.PatientScheduleSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (existing is null)
        {
            db.PatientScheduleSettings.Add(new PatientScheduleSettings
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
        var existing = await db.PatientScheduleSettings
            .FirstOrDefaultAsync(s => s.PatientId == patientId);

        if (existing is null)
        {
            db.PatientScheduleSettings.Add(new PatientScheduleSettings
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
        var existing = await db.PatientScheduleSettings
            .FirstOrDefaultAsync(s => s.PatientId == patientId);

        if (existing is not null)
        {
            db.PatientScheduleSettings.Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}
