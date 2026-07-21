using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IScheduleSettingsRepository
{
    Task<PatientScheduleSettings?> GetByTenantIdAsync(int tenantId);

    Task<PatientScheduleSettings?> GetByPatientIdAsync(int patientId);

    // Bulk lookup for the Transmission Schedule list view, to avoid an N+1
    // query per patient when resolving each patient's effective interval.
    Task<List<PatientScheduleSettings>> GetByPatientIdsAsync(IEnumerable<int> patientIds);

    Task UpsertTenantSettingsAsync(int tenantId, int intervalDays);

    Task UpsertPatientOverrideAsync(int patientId, int intervalDays);

    // Reverts the patient to the clinic-wide default interval.
    Task RemovePatientOverrideAsync(int patientId);
}
