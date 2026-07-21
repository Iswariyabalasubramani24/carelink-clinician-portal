using CareLink.Domain.Entities;

namespace CareLink.Application.Reports;

public record ReportDto(
    int Id,
    int PatientId,
    ReportType ReportType,
    DateTime GeneratedAt)
{
    public static ReportDto FromEntity(Report r) => new(r.Id, r.PatientId, r.ReportType, r.GeneratedAt);
}
