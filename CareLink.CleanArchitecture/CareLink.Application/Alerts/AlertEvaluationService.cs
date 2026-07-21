using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;

namespace CareLink.Application.Alerts;

// Alerts are evaluated deterministically from each patient's current data
// (no background job) and lazily persisted the first time a condition is
// detected, purely so acknowledge/snooze state has somewhere to live across
// requests - the same "generate on the fly, persist only what must survive"
// approach used for Sprint 4's transmission history.
public class AlertEvaluationService(
    IAlertRepository alertRepository,
    IClinicAlertSettingsRepository clinicAlertSettingsRepository,
    IPatientAlertSettingsRepository patientAlertSettingsRepository,
    IPatientRepository patientRepository) : IAlertEvaluationService
{
    private const int DisconnectedThresholdDays = 20;
    private const double IrregularHeartbeatProbability = 0.30;

    public async Task<List<Alert>> GetActiveAlertsForPatientAsync(Patient patient)
    {
        var clinicSettings = await clinicAlertSettingsRepository.GetByTenantIdAsync(patient.TenantId);
        var patientSettings = await patientAlertSettingsRepository.GetByPatientIdAsync(patient.Id);
        var existingAlerts = await alertRepository.GetByPatientIdAsync(patient.Id);

        return await EvaluateAsync(patient, clinicSettings, patientSettings, existingAlerts);
    }

    public async Task<List<Alert>> GetActiveAlertsForTenantAsync(int tenantId)
    {
        var patients = (await patientRepository.GetByTenantIdAsync(tenantId)).Where(p => p.IsActive).ToList();
        var clinicSettings = await clinicAlertSettingsRepository.GetByTenantIdAsync(tenantId);
        var allAlerts = await alertRepository.GetByTenantIdAsync(tenantId);

        var active = new List<Alert>();
        foreach (var patient in patients)
        {
            var patientSettings = await patientAlertSettingsRepository.GetByPatientIdAsync(patient.Id);
            var existingForPatient = allAlerts.Where(a => a.PatientId == patient.Id).ToList();
            active.AddRange(await EvaluateAsync(patient, clinicSettings, patientSettings, existingForPatient));
        }

        return active;
    }

    private async Task<List<Alert>> EvaluateAsync(
        Patient patient,
        List<ClinicAlertSettings> clinicSettings,
        List<PatientAlertSettings> patientSettings,
        List<Alert> existingAlerts)
    {
        var now = DateTime.UtcNow;
        var active = new List<Alert>();

        foreach (var alertType in Enum.GetValues<AlertType>())
        {
            if (!IsTriggered(patient, alertType))
            {
                continue;
            }

            var effectiveUrgency = ResolveEffectiveUrgency(patient, alertType, clinicSettings, patientSettings);
            if (effectiveUrgency == AlertUrgency.None)
            {
                continue;
            }

            var alertsForType = existingAlerts.Where(a => a.AlertType == alertType).ToList();

            // Once any instance of this alert type has been permanently acknowledged
            // for this patient, don't manufacture a new one. Our trigger conditions
            // are derived from static simulated data that never resolves on its own,
            // so without this a fresh row would reappear the instant the page is
            // reloaded, making "Acknowledge" pointless.
            if (alertsForType.Any(a => a.IsAcknowledged))
            {
                continue;
            }

            var existing = alertsForType.FirstOrDefault(a => !a.IsAcknowledged);
            if (existing is not null)
            {
                if (existing.SnoozedUntil is not null && existing.SnoozedUntil > now)
                {
                    continue; // temporarily suppressed
                }

                // Settings can change after an alert was first raised (clinic default
                // edited, or a patient override added/removed) - keep the persisted
                // row's urgency in sync rather than freezing it at creation time.
                if (existing.Urgency != effectiveUrgency)
                {
                    existing.Urgency = effectiveUrgency;
                    await alertRepository.UpdateAsync(existing);
                }

                active.Add(existing);
                continue;
            }

            var created = await alertRepository.AddAsync(new Alert
            {
                PatientId = patient.Id,
                TenantId = patient.TenantId,
                AlertType = alertType,
                Urgency = effectiveUrgency,
                TriggeredAt = now,
                IsAcknowledged = false
            });
            active.Add(created);
        }

        return active;
    }

    private static bool IsTriggered(Patient patient, AlertType alertType) => alertType switch
    {
        AlertType.LowBattery => patient.BatteryLevel.HasValue && patient.BatteryLevel.Value < 20,

        // Never-synced patients (LastSyncedAt null) are treated as disconnected,
        // consistent with the Sprint 4 dashboard's "Disconnected Monitors" widget.
        AlertType.DisconnectedMonitor => patient.LastSyncedAt is null
            || patient.LastSyncedAt < DateTime.UtcNow.AddDays(-DisconnectedThresholdDays),

        // No real device feed exists yet, so this is a deterministic pseudo-random
        // draw seeded by the patient's own Id - stable across requests, ~30% of
        // patients affected, same approach as the transmission history generator.
        AlertType.IrregularHeartbeat => new Random(patient.Id).NextDouble() < IrregularHeartbeatProbability,

        _ => false
    };

    // Shared by GetPatientAlertSettingsQuery and UpdatePatientAlertSettingsCommand
    // so both return the same resolved view of the CareAlert Notification tab.
    public static List<PatientAlertSettingsDto> ResolvePatientAlertSettings(
        Patient patient,
        List<ClinicAlertSettings> clinicSettings,
        List<PatientAlertSettings> patientSettings)
    {
        var result = new List<PatientAlertSettingsDto>();
        foreach (var alertType in Enum.GetValues<AlertType>())
        {
            var isOverride = patientSettings.Any(s => s.AlertType == alertType && s.IsOverride);
            var effectiveUrgency = ResolveEffectiveUrgency(patient, alertType, clinicSettings, patientSettings);
            result.Add(new PatientAlertSettingsDto(alertType, effectiveUrgency, isOverride));
        }

        return result;
    }

    // Public so alert-settings queries can resolve the same effective urgency
    // shown on the patient's CareAlert Notification tab without duplicating this logic.
    public static AlertUrgency ResolveEffectiveUrgency(
        Patient patient,
        AlertType alertType,
        List<ClinicAlertSettings> clinicSettings,
        List<PatientAlertSettings> patientSettings)
    {
        var overrideSetting = patientSettings.FirstOrDefault(s => s.AlertType == alertType && s.IsOverride);
        if (overrideSetting is not null)
        {
            return overrideSetting.Urgency;
        }

        var clinicDefault = clinicSettings.FirstOrDefault(s => s.AlertType == alertType);
        if (clinicDefault is not null)
        {
            return clinicDefault.DefaultUrgency;
        }

        // Fallback only reached if no clinic default row exists at all.
        return alertType switch
        {
            AlertType.LowBattery when patient.BatteryLevel < 10 => AlertUrgency.Red,
            AlertType.LowBattery => AlertUrgency.Yellow,
            AlertType.DisconnectedMonitor => AlertUrgency.Red,
            AlertType.IrregularHeartbeat => AlertUrgency.Yellow,
            _ => AlertUrgency.None
        };
    }
}
