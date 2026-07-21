using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ReportRepository(ApplicationDbContext db) : IReportRepository
{
    public async Task<List<Report>> GetByPatientIdAsync(int patientId)
    {
        return await db.Reports
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }

    public async Task<Report?> GetByIdAsync(int id, int tenantId)
    {
        return await db.Reports
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
    }

    public async Task<Report> AddAsync(Report report)
    {
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }
}
