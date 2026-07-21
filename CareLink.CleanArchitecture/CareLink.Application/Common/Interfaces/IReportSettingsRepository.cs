using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IReportSettingsRepository
{
    Task<ReportSettings?> GetByTenantIdAsync(int tenantId);

    Task<ReportSettings?> GetByPatientIdAsync(int patientId);

    Task UpsertTenantSettingsAsync(int tenantId, int intervalDays);

    Task UpsertPatientOverrideAsync(int patientId, int intervalDays);

    // Reverts the patient to the clinic-wide default interval.
    Task RemovePatientOverrideAsync(int patientId);
}
