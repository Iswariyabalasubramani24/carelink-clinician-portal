namespace CareLink.Application.Reports;

public record ReportSettingsDto(int IntervalDays);

public record PatientReportSettingsDto(int IntervalDays, bool IsOverride);
