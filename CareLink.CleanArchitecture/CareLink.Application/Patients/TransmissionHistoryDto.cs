namespace CareLink.Application.Patients;

public record TransmissionHistoryDto(
    DateTime Date,
    int HeartRate,
    decimal BatteryLevel);
