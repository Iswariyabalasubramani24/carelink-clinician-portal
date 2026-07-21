using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Schedule.Queries;

public class GetTransmissionScheduleQuery : IRequest<List<TransmissionScheduleEntryDto>>
{
    public int TenantId { get; set; }

    public GetTransmissionScheduleQuery() { }

    public GetTransmissionScheduleQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetTransmissionScheduleQueryHandler(
    IPatientRepository patientRepository,
    IScheduleSettingsRepository scheduleSettingsRepository)
    : IRequestHandler<GetTransmissionScheduleQuery, List<TransmissionScheduleEntryDto>>
{
    public async Task<List<TransmissionScheduleEntryDto>> Handle(GetTransmissionScheduleQuery request, CancellationToken cancellationToken)
    {
        var patients = await patientRepository.GetByTenantIdAsync(request.TenantId);
        var patientIds = patients.Select(p => p.Id).ToList();

        var overrides = await scheduleSettingsRepository.GetByPatientIdsAsync(patientIds);
        var overrideIntervalsByPatientId = overrides.ToDictionary(o => o.PatientId!.Value, o => o.IntervalDays);

        var clinicSettings = await scheduleSettingsRepository.GetByTenantIdAsync(request.TenantId);
        var clinicDefaultIntervalDays = clinicSettings?.IntervalDays ?? GetClinicScheduleSettingsQueryHandler.DefaultIntervalDays;

        return patients
            .Select(p => BuildEntry(p, overrideIntervalsByPatientId, clinicDefaultIntervalDays))
            // Soonest-due patients first; patients who have never synced (no
            // LastSyncedAt, so no computable next date) sort to the end.
            .OrderBy(e => e.NextScheduledDate ?? DateTime.MaxValue)
            .ToList();
    }

    private static TransmissionScheduleEntryDto BuildEntry(
        Patient patient, Dictionary<int, int> overrideIntervalsByPatientId, int clinicDefaultIntervalDays)
    {
        var intervalDays = overrideIntervalsByPatientId.TryGetValue(patient.Id, out var overrideInterval)
            ? overrideInterval
            : clinicDefaultIntervalDays;

        return new TransmissionScheduleEntryDto(
            patient.Id,
            $"{patient.FirstName} {patient.LastName}",
            patient.LastSyncedAt,
            intervalDays,
            CalculateNextScheduledDate(patient.LastSyncedAt, intervalDays));
    }

    public static DateTime? CalculateNextScheduledDate(DateTime? lastSyncedAt, int intervalDays) =>
        lastSyncedAt?.AddDays(intervalDays);
}
