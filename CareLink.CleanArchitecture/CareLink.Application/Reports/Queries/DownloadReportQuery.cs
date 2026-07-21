using System.Text.Json;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Reports.Queries;

public class DownloadReportQuery : IRequest<ReportFileDto>
{
    public int ReportId { get; set; }

    public int TenantId { get; set; }

    public DownloadReportQuery() { }

    public DownloadReportQuery(int reportId, int tenantId)
    {
        ReportId = reportId;
        TenantId = tenantId;
    }
}

public class DownloadReportQueryHandler(IReportRepository reportRepository, IReportPdfGenerator reportPdfGenerator)
    : IRequestHandler<DownloadReportQuery, ReportFileDto>
{
    public async Task<ReportFileDto> Handle(DownloadReportQuery request, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(request.ReportId, request.TenantId);
        if (report is null)
        {
            throw new ReportNotFoundException();
        }

        var snapshot = JsonSerializer.Deserialize<ReportSnapshotDto>(report.DataSnapshot)
            ?? throw new ReportNotFoundException();

        // Regenerated from the stored snapshot each time rather than cached as
        // PDF bytes - keeps storage small and guarantees the download matches
        // the data captured at generation time even if the PDF layout evolves.
        var content = reportPdfGenerator.Generate(report.ReportType, report.GeneratedAt, snapshot);
        var fileName = $"{report.ReportType}_{snapshot.LastName}_{snapshot.MedicalRecordNumber}_{report.GeneratedAt:yyyyMMdd}.pdf";

        return new ReportFileDto(content, fileName);
    }
}
