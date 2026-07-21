using CareLink.Domain.Entities;

namespace CareLink.Application.AuditLogs;

public record AuditLogDto(
    int Id,
    int ClinicianId,
    string Action,
    string EntityType,
    int EntityId,
    DateTime Timestamp,
    string? Details)
{
    public static AuditLogDto FromEntity(AuditLog a) => new(
        a.Id,
        a.ClinicianId,
        a.Action,
        a.EntityType,
        a.EntityId,
        a.Timestamp,
        a.Details);
}

public record PagedAuditLogsDto(List<AuditLogDto> Items, int TotalCount, int Page, int PageSize);
