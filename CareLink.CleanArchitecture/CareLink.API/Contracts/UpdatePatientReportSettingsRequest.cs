namespace CareLink.API.Contracts;

public record UpdatePatientReportSettingsRequest(bool UseOverride, int IntervalDays);
