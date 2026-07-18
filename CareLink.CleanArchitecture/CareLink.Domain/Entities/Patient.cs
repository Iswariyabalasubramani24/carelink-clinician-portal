namespace CareLink.Domain.Entities;

public class Patient
{
    public int Id { get; set; }

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

    public DateTime? LastSyncedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
