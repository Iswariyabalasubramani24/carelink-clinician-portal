using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class PatientAlertSettingsRepository(ApplicationDbContext db) : IPatientAlertSettingsRepository
{
    public async Task<List<PatientAlertSettings>> GetByPatientIdAsync(int patientId)
    {
        return await db.PatientAlertSettings
            .AsNoTracking()
            .Where(s => s.PatientId == patientId)
            .ToListAsync();
    }

    public async Task ReplaceOverridesAsync(int patientId, List<(AlertType AlertType, AlertUrgency Urgency)> overrides)
    {
        var existing = await db.PatientAlertSettings
            .Where(s => s.PatientId == patientId)
            .ToListAsync();

        db.PatientAlertSettings.RemoveRange(existing);

        foreach (var (alertType, urgency) in overrides)
        {
            db.PatientAlertSettings.Add(new PatientAlertSettings
            {
                PatientId = patientId,
                AlertType = alertType,
                Urgency = urgency,
                IsOverride = true
            });
        }

        await db.SaveChangesAsync();
    }
}
