using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Patients.Queries;

public class GetPatientTransmissionHistoryQuery : IRequest<List<TransmissionHistoryDto>>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientTransmissionHistoryQuery() { }

    public GetPatientTransmissionHistoryQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientTransmissionHistoryQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetPatientTransmissionHistoryQuery, List<TransmissionHistoryDto>>
{
    public async Task<List<TransmissionHistoryDto>> Handle(GetPatientTransmissionHistoryQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        return Generate(patient);
    }

    // There's no real device-integration feed yet, so history is generated
    // deterministically from the patient's Id rather than stored at creation
    // time: it needs no migration/backfill, it works retroactively for every
    // patient already seeded in earlier sprints, and the same patient always
    // produces the same series across requests/page reloads.
    private static List<TransmissionHistoryDto> Generate(Patient patient)
    {
        var random = new Random(patient.Id);
        var pointCount = random.Next(10, 16); // 10-15 inclusive
        var today = DateTime.UtcNow.Date;

        var baseHeartRate = patient.LastHeartRate ?? 72;
        var baseBattery = patient.BatteryLevel ?? 90m;

        var points = new List<TransmissionHistoryDto>();

        for (var i = 0; i < pointCount; i++)
        {
            var daysAgo = 90 - (int)Math.Round(i * 90.0 / (pointCount - 1));
            var date = today.AddDays(-daysAgo);
            var isMostRecentPoint = i == pointCount - 1;

            if (isMostRecentPoint)
            {
                // Anchor the newest simulated reading to the patient's current
                // values, so the chart's endpoint matches what's shown elsewhere.
                points.Add(new TransmissionHistoryDto(date, baseHeartRate, baseBattery));
                continue;
            }

            var heartRateJitter = random.Next(-6, 7);
            var heartRate = Math.Max(40, baseHeartRate + heartRateJitter);

            // Batteries deplete gradually, so older points read higher.
            var ageBoost = (pointCount - 1 - i) * 0.6m;
            var batteryJitter = random.Next(-2, 3) * 0.1m;
            var batteryLevel = Math.Clamp(baseBattery + ageBoost + batteryJitter, 0m, 100m);

            points.Add(new TransmissionHistoryDto(date, heartRate, batteryLevel));
        }

        return points;
    }
}
