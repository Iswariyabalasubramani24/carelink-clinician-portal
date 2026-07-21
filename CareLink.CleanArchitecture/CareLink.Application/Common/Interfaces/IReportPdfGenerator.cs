using CareLink.Application.Reports;
using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IReportPdfGenerator
{
    byte[] Generate(ReportType reportType, DateTime generatedAt, ReportSnapshotDto snapshot);
}
