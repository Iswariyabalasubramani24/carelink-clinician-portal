using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Patients.Commands;

public class CreatePatientCommand : IRequest<PatientDto>
{
    public int TenantId { get; set; }
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

public class CreatePatientCommandHandler(IPatientRepository patientRepository) : IRequestHandler<CreatePatientCommand, PatientDto>
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

        return PatientDto.FromEntity(created);
    }
}
