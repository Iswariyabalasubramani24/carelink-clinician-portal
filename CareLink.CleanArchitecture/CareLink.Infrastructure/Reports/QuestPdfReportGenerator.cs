using CareLink.Application.Common.Interfaces;
using CareLink.Application.Reports;
using CareLink.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareLink.Infrastructure.Reports;

public class QuestPdfReportGenerator : IReportPdfGenerator
{
    public byte[] Generate(ReportType reportType, DateTime generatedAt, ReportSnapshotDto snapshot)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(header => ComposeHeader(header, reportType, generatedAt, snapshot));
                page.Content().Element(content => ComposeContent(content, reportType, snapshot));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReportType reportType, DateTime generatedAt, ReportSnapshotDto snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text(ReportTitle(reportType)).FontSize(18).Bold();
            column.Item().PaddingTop(4).Text($"{snapshot.FirstName} {snapshot.LastName}  |  MRN: {snapshot.MedicalRecordNumber}").FontSize(12).SemiBold();
            column.Item().Text(snapshot.TenantName).FontSize(10);
            column.Item().Text($"Generated: {generatedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9);
            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ComposeContent(IContainer container, ReportType reportType, ReportSnapshotDto snapshot)
    {
        container.PaddingTop(10).Column(column =>
        {
            column.Spacing(14);

            switch (reportType)
            {
                case ReportType.FullReport:
                    column.Item().Element(e => ComposePatientInfo(e, snapshot));
                    column.Item().Element(e => ComposeEquipmentInfo(e, snapshot));
                    column.Item().Element(e => ComposeTransmissionHistoryTable(e, snapshot));
                    column.Item().Element(e => ComposeAlertsTable(e, snapshot, "Alert History"));
                    break;

                case ReportType.SummaryReport:
                    column.Item().Element(e => ComposePatientInfo(e, snapshot));
                    column.Item().Element(e => ComposeLatestReadings(e, snapshot));
                    column.Item().Element(e => ComposeAlertCounts(e, snapshot));
                    break;

                case ReportType.EventReport:
                    column.Item().Element(e => ComposePatientInfo(e, snapshot));
                    column.Item().Element(e => ComposeAlertsTable(e, snapshot, "Alert Details"));
                    break;
            }
        });
    }

    private static void ComposePatientInfo(IContainer container, ReportSnapshotDto snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text("Patient Information").FontSize(13).Bold();
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Date of Birth: {snapshot.DateOfBirth:yyyy-MM-dd}");
                row.RelativeItem().Text($"MRN: {snapshot.MedicalRecordNumber}");
            });
        });
    }

    private static void ComposeEquipmentInfo(IContainer container, ReportSnapshotDto snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text("Equipment").FontSize(13).Bold();
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Device Type: {snapshot.DeviceType}");
                row.RelativeItem().Text($"Manufacturer: {snapshot.DeviceManufacturer ?? "N/A"}");
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Model: {snapshot.DeviceModel ?? "N/A"}");
                row.RelativeItem().Text($"Serial Number: {snapshot.DeviceSerialNumber}");
            });
            column.Item().Text($"Implant Date: {snapshot.ImplantDate:yyyy-MM-dd}");
        });
    }

    private static void ComposeLatestReadings(IContainer container, ReportSnapshotDto snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text("Latest Readings").FontSize(13).Bold();
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Heart Rate: {(snapshot.LastHeartRate.HasValue ? $"{snapshot.LastHeartRate} bpm" : "N/A")}");
                row.RelativeItem().Text($"Battery Level: {(snapshot.BatteryLevel.HasValue ? $"{snapshot.BatteryLevel:0.0}%" : "N/A")}");
            });
            column.Item().Text($"Last Synced: {(snapshot.LastSyncedAt.HasValue ? snapshot.LastSyncedAt.Value.ToString("yyyy-MM-dd HH:mm") : "N/A")}");
        });
    }

    private static void ComposeAlertCounts(IContainer container, ReportSnapshotDto snapshot)
    {
        var total = snapshot.Alerts.Count;
        var unacknowledged = snapshot.Alerts.Count(a => !a.IsAcknowledged);
        var red = snapshot.Alerts.Count(a => a.Urgency == nameof(AlertUrgency.Red));
        var yellow = snapshot.Alerts.Count(a => a.Urgency == nameof(AlertUrgency.Yellow));

        container.Column(column =>
        {
            column.Item().Text("Alert Summary").FontSize(13).Bold();
            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Total Alerts: {total}");
                row.RelativeItem().Text($"Unacknowledged: {unacknowledged}");
            });
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Red (Urgent): {red}");
                row.RelativeItem().Text($"Yellow (Warning): {yellow}");
            });
        });
    }

    private static void ComposeTransmissionHistoryTable(IContainer container, ReportSnapshotDto snapshot)
    {
        container.Column(column =>
        {
            column.Item().Text("Transmission History").FontSize(13).Bold();

            if (snapshot.TransmissionHistory.Count == 0)
            {
                column.Item().PaddingTop(4).Text("No transmission history available.").Italic();
                return;
            }

            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date").Bold();
                    header.Cell().Element(CellStyle).Text("Heart Rate (bpm)").Bold();
                    header.Cell().Element(CellStyle).Text("Battery (%)").Bold();
                });

                foreach (var point in snapshot.TransmissionHistory.OrderBy(p => p.Date))
                {
                    table.Cell().Element(CellStyle).Text(point.Date.ToString("yyyy-MM-dd"));
                    table.Cell().Element(CellStyle).Text(point.HeartRate.ToString());
                    table.Cell().Element(CellStyle).Text($"{point.BatteryLevel:0.0}");
                }
            });
        });
    }

    private static void ComposeAlertsTable(IContainer container, ReportSnapshotDto snapshot, string title)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(13).Bold();

            if (snapshot.Alerts.Count == 0)
            {
                column.Item().PaddingTop(4).Text("No alerts recorded for this patient.").Italic();
                return;
            }

            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Type").Bold();
                    header.Cell().Element(CellStyle).Text("Urgency").Bold();
                    header.Cell().Element(CellStyle).Text("Triggered At").Bold();
                    header.Cell().Element(CellStyle).Text("Status").Bold();
                });

                foreach (var alert in snapshot.Alerts.OrderByDescending(a => a.TriggeredAt))
                {
                    var status = alert.IsAcknowledged
                        ? $"Acknowledged {alert.AcknowledgedAt:yyyy-MM-dd HH:mm}"
                        : "Active";

                    table.Cell().Element(CellStyle).Text(alert.AlertType);
                    table.Cell().Element(CellStyle).Text(alert.Urgency);
                    table.Cell().Element(CellStyle).Text(alert.TriggeredAt.ToString("yyyy-MM-dd HH:mm"));
                    table.Cell().Element(CellStyle).Text(status);
                }
            });
        });
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.PaddingVertical(4).PaddingHorizontal(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    }

    private static string ReportTitle(ReportType reportType) => reportType switch
    {
        ReportType.FullReport => "Full Patient Report",
        ReportType.SummaryReport => "Summary Report",
        ReportType.EventReport => "Event Report",
        _ => "Report"
    };
}
