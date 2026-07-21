using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IPatientAlertSettingsRepository
{
    Task<List<PatientAlertSettings>> GetByPatientIdAsync(int patientId);

    // Replaces all of a patient's override rows in one go. An empty list
    // reverts the patient to the clinic default for every alert type.
    Task ReplaceOverridesAsync(int patientId, List<(AlertType AlertType, AlertUrgency Urgency)> overrides);
}
