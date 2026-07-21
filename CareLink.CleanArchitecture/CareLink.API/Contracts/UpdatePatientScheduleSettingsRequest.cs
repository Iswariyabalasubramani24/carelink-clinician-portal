namespace CareLink.API.Contracts;

public record UpdatePatientScheduleSettingsRequest(bool UseOverride, int IntervalDays);
