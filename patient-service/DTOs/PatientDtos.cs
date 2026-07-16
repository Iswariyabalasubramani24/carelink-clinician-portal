using System.ComponentModel.DataAnnotations;
using CareLink.PatientService.Models;

namespace CareLink.PatientService.DTOs;

public record PatientDto(
    int Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? PhoneNumber,
    string? Email,
    CardiacDeviceType DeviceType,
    string? DeviceManufacturer,
    string? DeviceModel,
    string DeviceSerialNumber,
    DateOnly ImplantDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class CreatePatientDto
{
    [Required, MaxLength(20)]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [Required]
    public CardiacDeviceType DeviceType { get; set; }

    [MaxLength(100)]
    public string? DeviceManufacturer { get; set; }

    [MaxLength(100)]
    public string? DeviceModel { get; set; }

    [Required, MaxLength(100)]
    public string DeviceSerialNumber { get; set; } = string.Empty;

    [Required]
    public DateOnly ImplantDate { get; set; }
}

public class UpdatePatientDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [Required]
    public CardiacDeviceType DeviceType { get; set; }

    [MaxLength(100)]
    public string? DeviceManufacturer { get; set; }

    [MaxLength(100)]
    public string? DeviceModel { get; set; }

    [Required]
    public DateOnly ImplantDate { get; set; }
}
