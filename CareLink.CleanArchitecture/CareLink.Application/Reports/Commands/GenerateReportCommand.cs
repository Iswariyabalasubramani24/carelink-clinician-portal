using System.Text.Json;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients.Queries;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Reports.Commands;

public class GenerateReportCommand : IRequest<ReportDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public int ClinicianId { get; set; }

    public ReportType ReportType { get; set; }

    public GenerateReportCommand() { }

    public GenerateReportCommand(int patientId, int tenantId, ReportType reportType, int clinicianId = 0)
    {
        PatientId = patientId;
        TenantId = tenantId;
        ReportType = reportType;
        ClinicianId = clinicianId;
    }
}

public class GenerateReportCommandHandler(
    IPatientRepository patientRepository,
    ITenantRepository tenantRepository,
    IAlertRepository alertRepository,
    IAlertEvaluationService alertEvaluationService,
    IReportRepository reportRepository,
    IAuditLogger auditLogger,
    IMediator mediator)
    : IRequestHandler<GenerateReportCommand, ReportDto>
{
    public async Task<ReportDto> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var tenant = await tenantRepository.GetByIdAsync(request.TenantId);

        var transmissionHistory = await mediator.Send(
            new GetPatientTransmissionHistoryQuery(request.PatientId, request.TenantId), cancellationToken);

        // Ensure any currently-triggered alert conditions are persisted before
        // capturing the full alert history, so the snapshot doesn't miss an
        // alert that's active right now but has never been saved before.
        await alertEvaluationService.GetActiveAlertsForPatientAsync(patient);
        var alerts = await alertRepository.GetByPatientIdAsync(request.PatientId);

        var snapshot = new ReportSnapshotDto(
            TenantName: tenant?.Name ?? string.Empty,
            PatientId: patient.Id,
            MedicalRecordNumber: patient.MedicalRecordNumber,
            FirstName: patient.FirstName,
            LastName: patient.LastName,
            DateOfBirth: patient.DateOfBirth,
            DeviceType: patient.DeviceType.ToString(),
            DeviceManufacturer: patient.DeviceManufacturer,
            DeviceModel: patient.DeviceModel,
            DeviceSerialNumber: patient.DeviceSerialNumber,
            ImplantDate: patient.ImplantDate,
            BatteryLevel: patient.BatteryLevel,
            LastHeartRate: patient.LastHeartRate,
            LastSyncedAt: patient.LastSyncedAt,
            TransmissionHistory: transmissionHistory,
            Alerts: alerts.Select(a => new ReportAlertDto(
                a.AlertType.ToString(),
                a.Urgency.ToString(),
                a.TriggeredAt,
                a.IsAcknowledged,
                a.AcknowledgedAt)).ToList());

        var report = new Report
        {
            PatientId = request.PatientId,
            TenantId = request.TenantId,
            ReportType = request.ReportType,
            GeneratedAt = DateTime.UtcNow,
            DataSnapshot = JsonSerializer.Serialize(snapshot)
        };

        var created = await reportRepository.AddAsync(report);

        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, "ReportGenerated", "Report", created.Id,
            $"{request.ReportType} for patient {request.PatientId}");

        return ReportDto.FromEntity(created);
    }
}
