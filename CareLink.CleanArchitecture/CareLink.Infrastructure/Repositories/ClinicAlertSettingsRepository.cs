using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ClinicAlertSettingsRepository(ApplicationDbContext db) : IClinicAlertSettingsRepository
{
    public async Task<List<ClinicAlertSettings>> GetByTenantIdAsync(int tenantId)
    {
        return await db.ClinicAlertSettings
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task UpsertAsync(int tenantId, AlertType alertType, AlertUrgency defaultUrgency)
    {
        var existing = await db.ClinicAlertSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.AlertType == alertType);

        if (existing is null)
        {
            db.ClinicAlertSettings.Add(new ClinicAlertSettings
            {
                TenantId = tenantId,
                AlertType = alertType,
                DefaultUrgency = defaultUrgency
            });
        }
        else
        {
            existing.DefaultUrgency = defaultUrgency;
        }

        await db.SaveChangesAsync();
    }
}
