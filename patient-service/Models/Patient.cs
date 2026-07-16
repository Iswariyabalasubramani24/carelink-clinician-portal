using System.ComponentModel.DataAnnotations;

namespace CareLink.PatientService.Models;

public enum CardiacDeviceType
{
    Pacemaker,
    ImplantableCardioverterDefibrillator,
    CardiacResynchronizationTherapy,
    LoopRecorder
}

public class Patient
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    public CardiacDeviceType DeviceType { get; set; }

    [MaxLength(100)]
    public string? DeviceManufacturer { get; set; }

    [MaxLength(100)]
    public string? DeviceModel { get; set; }

    [Required, MaxLength(100)]
    public string DeviceSerialNumber { get; set; } = string.Empty;

    public DateOnly ImplantDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
