using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class AlertRepository(ApplicationDbContext db) : IAlertRepository
{
    public async Task<List<Alert>> GetByTenantIdAsync(int tenantId)
    {
        return await db.Alerts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetByPatientIdAsync(int patientId)
    {
        return await db.Alerts
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .ToListAsync();
    }

    public async Task<Alert?> GetByIdAsync(int id, int tenantId)
    {
        return await db.Alerts
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);
    }

    public async Task<Alert> AddAsync(Alert alert)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync();
        return alert;
    }

    public async Task UpdateAsync(Alert alert)
    {
        db.Alerts.Update(alert);
        await db.SaveChangesAsync();
    }
}
