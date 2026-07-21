namespace CareLink.Application.Schedule;

public record ScheduleSettingsDto(int IntervalDays);

public record PatientScheduleSettingsDto(int IntervalDays, bool IsOverride);
