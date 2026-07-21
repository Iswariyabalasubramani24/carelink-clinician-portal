namespace CareLink.Application.Schedule;

public record TransmissionScheduleEntryDto(
    int PatientId,
    string PatientName,
    DateTime? LastSyncedAt,
    int IntervalDays,
    DateTime? NextScheduledDate);
