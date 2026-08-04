using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Patients.Commands;

// Patients were previously create-only: no way to fix a typo'd MRN or update
// contact/device details after the initial registration. Editable fields
// mirror CreatePatientCommand, minus device telemetry (BatteryLevel,
// LastHeartRate, LastSyncedAt) which is device-reported, not clinician-entered.
public class UpdatePatientCommand : IRequest<PatientDto>
{
    public int PatientId { get; set; }
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
}

public class UpdatePatientCommandHandler(IPatientRepository patientRepository, IAuditLogger auditLogger)
    : IRequestHandler<UpdatePatientCommand, PatientDto>
{
    public async Task<PatientDto> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId)
            ?? throw new PatientNotFoundException();

        patient.MedicalRecordNumber = request.MedicalRecordNumber;
        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.PhoneNumber = request.PhoneNumber;
        patient.Email = request.Email;
        patient.DeviceType = request.DeviceType;
        patient.DeviceManufacturer = request.DeviceManufacturer;
        patient.DeviceModel = request.DeviceModel;
        patient.DeviceSerialNumber = request.DeviceSerialNumber;
        patient.ImplantDate = request.ImplantDate;
        patient.UpdatedAt = DateTime.UtcNow;

        await patientRepository.UpdateAsync(patient);

        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, "PatientUpdated", "Patient", patient.Id,
            $"MRN {patient.MedicalRecordNumber}");

        return PatientDto.FromEntity(patient);
    }
}
