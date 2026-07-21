using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IReportRepository
{
    Task<List<Report>> GetByPatientIdAsync(int patientId);

    // Scoped to tenantId so a report ID from one hospital can never be
    // downloaded by a clinician authenticated against a different tenant.
    Task<Report?> GetByIdAsync(int id, int tenantId);

    Task<Report> AddAsync(Report report);
}
