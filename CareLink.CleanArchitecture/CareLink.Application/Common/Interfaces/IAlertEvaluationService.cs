using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IAlertEvaluationService
{
    Task<List<Alert>> GetActiveAlertsForPatientAsync(Patient patient);

    Task<List<Alert>> GetActiveAlertsForTenantAsync(int tenantId);
}
