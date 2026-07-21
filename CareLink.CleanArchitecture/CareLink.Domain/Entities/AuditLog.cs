namespace CareLink.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public DateTime Timestamp { get; set; }

    public string? Details { get; set; }
}
