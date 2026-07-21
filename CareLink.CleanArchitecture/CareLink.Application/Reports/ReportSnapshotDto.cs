using CareLink.Application.Patients;

namespace CareLink.Application.Reports;

// Captures ALL data needed to render any report type in one JSON blob at
// generation time - the PDF renderer decides which sections to show based on
// ReportType. Avoids building/duplicating three separate snapshot shapes and
// keeps re-downloads reproducible even if the patient's live data changes later.
public record ReportSnapshotDto(
    string TenantName,
    int PatientId,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string DeviceType,
    string? DeviceManufacturer,
    string? DeviceModel,
    string DeviceSerialNumber,
    DateTime ImplantDate,
    decimal? BatteryLevel,
    int? LastHeartRate,
    DateTime? LastSyncedAt,
    List<TransmissionHistoryDto> TransmissionHistory,
    List<ReportAlertDto> Alerts);

public record ReportAlertDto(
    string AlertType,
    string Urgency,
    DateTime TriggeredAt,
    bool IsAcknowledged,
    DateTime? AcknowledgedAt);
