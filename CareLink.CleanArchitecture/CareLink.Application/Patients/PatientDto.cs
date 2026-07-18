using CareLink.Domain.Entities;

namespace CareLink.Application.Patients;

public record PatientDto(
    int Id,
    int TenantId,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? PhoneNumber,
    string? Email,
    DeviceType DeviceType,
    string? DeviceManufacturer,
    string? DeviceModel,
    string DeviceSerialNumber,
    DateTime ImplantDate,
    decimal? BatteryLevel,
    int? LastHeartRate,
    DateTime? LastSyncedAt,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PatientDto FromEntity(Patient p) => new(
        p.Id,
        p.TenantId,
        p.MedicalRecordNumber,
        p.FirstName,
        p.LastName,
        p.DateOfBirth,
        p.PhoneNumber,
        p.Email,
        p.DeviceType,
        p.DeviceManufacturer,
        p.DeviceModel,
        p.DeviceSerialNumber,
        p.ImplantDate,
        p.BatteryLevel,
        p.LastHeartRate,
        p.LastSyncedAt,
        p.IsActive,
        p.CreatedAt,
        p.UpdatedAt);
}
