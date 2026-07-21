using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IClinicAlertSettingsRepository
{
    Task<List<ClinicAlertSettings>> GetByTenantIdAsync(int tenantId);

    Task UpsertAsync(int tenantId, AlertType alertType, AlertUrgency defaultUrgency);
}
