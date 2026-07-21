using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IAlertRepository
{
    Task<List<Alert>> GetByTenantIdAsync(int tenantId);

    Task<List<Alert>> GetByPatientIdAsync(int patientId);

    // Scoped to tenantId so an alert ID from one hospital can never be acted
    // on by a clinician authenticated against a different tenant.
    Task<Alert?> GetByIdAsync(int id, int tenantId);

    Task<Alert> AddAsync(Alert alert);

    Task UpdateAsync(Alert alert);
}
