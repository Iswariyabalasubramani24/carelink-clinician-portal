using CareLink.Application.Common.Interfaces;
using CareLink.Application.Dashboard.Queries;
using CareLink.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace CareLink.Application.Patients.Commands;

public class CreatePatientCommand : IRequest<PatientDto>
{
    public int TenantId { get; set; }
    public int ClinicianId { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DeviceType DeviceType { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceModel { get; set; }
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public DateTime ImplantDate { get; set; }
    public decimal? BatteryLevel { get; set; }
    public int? LastHeartRate { get; set; }
}

public class CreatePatientCommandHandler(IPatientRepository patientRepository, IAuditLogger auditLogger, IDistributedCache cache)
    : IRequestHandler<CreatePatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient
        {
            TenantId = request.TenantId,
            MedicalRecordNumber = request.MedicalRecordNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            DeviceType = request.DeviceType,
            DeviceManufacturer = request.DeviceManufacturer,
            DeviceModel = request.DeviceModel,
            DeviceSerialNumber = request.DeviceSerialNumber,
            ImplantDate = request.ImplantDate,
            BatteryLevel = request.BatteryLevel,
            LastHeartRate = request.LastHeartRate,
            CreatedAt = DateTime.UtcNow
        };

        var created = await patientRepository.AddAsync(patient);

        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, "PatientCreated", "Patient", created.Id,
            $"MRN {created.MedicalRecordNumber}");

        // The dashboard's New Patients / Total Active Patients counts are now
        // stale - drop the cached summary rather than waiting out the TTL.
        await cache.RemoveAsync(GetDashboardSummaryQueryHandler.CacheKey(request.TenantId), cancellationToken);

        return PatientDto.FromEntity(created);
    }
}
