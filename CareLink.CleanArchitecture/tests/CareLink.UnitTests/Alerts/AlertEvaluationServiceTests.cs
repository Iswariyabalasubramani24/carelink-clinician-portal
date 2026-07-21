using CareLink.Application.Alerts;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Alerts;

public class AlertEvaluationServiceTests
{
    private static Patient MakePatient(int id, decimal? batteryLevel = null, DateTime? lastSyncedAt = null, int tenantId = 1) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = "Test",
        LastName = $"Patient{id}",
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.ICD,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1),
        CreatedAt = DateTime.UtcNow.AddYears(-1),
        BatteryLevel = batteryLevel,
        LastSyncedAt = lastSyncedAt,
        IsActive = true
    };

    private static ClinicAlertSettings ClinicDefault(int tenantId, AlertType type, AlertUrgency urgency) => new()
    {
        TenantId = tenantId,
        AlertType = type,
        DefaultUrgency = urgency
    };

    private sealed class Mocks
    {
        public Mock<IAlertRepository> AlertRepo { get; } = new();
        public Mock<IClinicAlertSettingsRepository> ClinicRepo { get; } = new();
        public Mock<IPatientAlertSettingsRepository> PatientSettingsRepo { get; } = new();
        public Mock<IPatientRepository> PatientRepo { get; } = new();

        public AlertEvaluationService BuildService() =>
            new(AlertRepo.Object, ClinicRepo.Object, PatientSettingsRepo.Object, PatientRepo.Object);
    }

    private static Mocks MakeMocks(int tenantId, List<ClinicAlertSettings>? clinicSettings = null, List<PatientAlertSettings>? patientSettings = null, List<Alert>? existingAlerts = null)
    {
        var mocks = new Mocks();
        mocks.ClinicRepo.Setup(r => r.GetByTenantIdAsync(tenantId)).ReturnsAsync(clinicSettings ?? new List<ClinicAlertSettings>());
        mocks.PatientSettingsRepo.Setup(r => r.GetByPatientIdAsync(It.IsAny<int>())).ReturnsAsync(patientSettings ?? new List<PatientAlertSettings>());
        mocks.AlertRepo.Setup(r => r.GetByPatientIdAsync(It.IsAny<int>())).ReturnsAsync(existingAlerts ?? new List<Alert>());
        mocks.AlertRepo.Setup(r => r.AddAsync(It.IsAny<Alert>())).ReturnsAsync((Alert a) => a);
        return mocks;
    }

    [Fact]
    public async Task LowBattery_Under20Percent_Triggers()
    {
        var patient = MakePatient(1, batteryLevel: 15m);
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Yellow),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        var lowBattery = Assert.Single(active, a => a.AlertType == AlertType.LowBattery);
        Assert.Equal(AlertUrgency.Yellow, lowBattery.Urgency);
    }

    [Fact]
    public async Task LowBattery_20PercentOrAbove_DoesNotTrigger()
    {
        var patient = MakePatient(1, batteryLevel: 20m);
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Yellow),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.DoesNotContain(active, a => a.AlertType == AlertType.LowBattery);
    }

    [Fact]
    public async Task LowBattery_NoClinicSettingsConfigured_FallsBackToNaturalSeverity()
    {
        var redPatient = MakePatient(1, batteryLevel: 5m);
        var yellowPatient = MakePatient(2, batteryLevel: 15m);
        var mocks = MakeMocks(1); // no clinic settings at all

        var redActive = await mocks.BuildService().GetActiveAlertsForPatientAsync(redPatient);
        var yellowActive = await mocks.BuildService().GetActiveAlertsForPatientAsync(yellowPatient);

        Assert.Equal(AlertUrgency.Red, Assert.Single(redActive, a => a.AlertType == AlertType.LowBattery).Urgency);
        Assert.Equal(AlertUrgency.Yellow, Assert.Single(yellowActive, a => a.AlertType == AlertType.LowBattery).Urgency);
    }

    [Fact]
    public async Task DisconnectedMonitor_NeverSynced_Triggers()
    {
        var patient = MakePatient(1, lastSyncedAt: null);
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.None),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.Red),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.Single(active, a => a.AlertType == AlertType.DisconnectedMonitor);
    }

    [Fact]
    public async Task DisconnectedMonitor_SyncedWithinThreshold_DoesNotTrigger()
    {
        var patient = MakePatient(1, lastSyncedAt: DateTime.UtcNow.AddDays(-5));
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.None),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.Red),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.DoesNotContain(active, a => a.AlertType == AlertType.DisconnectedMonitor);
    }

    [Fact]
    public async Task DisconnectedMonitor_SyncedOver20DaysAgo_Triggers()
    {
        var patient = MakePatient(1, lastSyncedAt: DateTime.UtcNow.AddDays(-25));
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.None),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.Red),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.Single(active, a => a.AlertType == AlertType.DisconnectedMonitor);
    }

    [Fact]
    public async Task IrregularHeartbeat_SamePatientId_AlwaysProducesTheSameResult()
    {
        var patient = MakePatient(42, batteryLevel: 90m, lastSyncedAt: DateTime.UtcNow.AddDays(-1));
        var settings = new List<ClinicAlertSettings> { ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.Yellow) };

        var firstRun = await MakeMocks(1, settings).BuildService().GetActiveAlertsForPatientAsync(patient);
        var secondRun = await MakeMocks(1, settings).BuildService().GetActiveAlertsForPatientAsync(patient);

        var firstHasHeartbeat = firstRun.Any(a => a.AlertType == AlertType.IrregularHeartbeat);
        var secondHasHeartbeat = secondRun.Any(a => a.AlertType == AlertType.IrregularHeartbeat);
        Assert.Equal(firstHasHeartbeat, secondHasHeartbeat);
    }

    [Fact]
    public async Task IrregularHeartbeat_AcrossManyPatients_TriggersForRoughlyThirtyPercent()
    {
        var settings = new List<ClinicAlertSettings> { ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.Yellow) };

        var triggeredCount = 0;
        const int sampleSize = 300;

        for (var id = 1; id <= sampleSize; id++)
        {
            var patient = MakePatient(id, batteryLevel: 90m, lastSyncedAt: DateTime.UtcNow.AddDays(-1));
            var active = await MakeMocks(1, settings).BuildService().GetActiveAlertsForPatientAsync(patient);
            if (active.Any(a => a.AlertType == AlertType.IrregularHeartbeat))
            {
                triggeredCount++;
            }
        }

        var proportion = (double)triggeredCount / sampleSize;
        Assert.InRange(proportion, 0.15, 0.45);
    }

    [Fact]
    public async Task ClinicDefaultNone_SuppressesAlertEvenWhenConditionIsMet()
    {
        var patient = MakePatient(1, batteryLevel: 5m); // would otherwise be a Red LowBattery alert
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.None),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.Empty(active);
    }

    [Fact]
    public async Task PatientOverride_TakesPrecedenceOverClinicDefault()
    {
        var patient = MakePatient(1, batteryLevel: 5m);
        var clinicSettings = new List<ClinicAlertSettings> { ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Yellow) };
        var patientSettings = new List<PatientAlertSettings>
        {
            new() { PatientId = 1, AlertType = AlertType.LowBattery, Urgency = AlertUrgency.Red, IsOverride = true }
        };
        var mocks = MakeMocks(1, clinicSettings, patientSettings);

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.Equal(AlertUrgency.Red, Assert.Single(active, a => a.AlertType == AlertType.LowBattery).Urgency);
    }

    [Fact]
    public async Task ExistingUnacknowledgedAlert_IsReused_NotDuplicated()
    {
        var patient = MakePatient(1, batteryLevel: 5m);
        var existing = new Alert
        {
            Id = 99,
            PatientId = 1,
            TenantId = 1,
            AlertType = AlertType.LowBattery,
            Urgency = AlertUrgency.Red,
            TriggeredAt = DateTime.UtcNow.AddDays(-3),
            IsAcknowledged = false
        };
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Red),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        }, existingAlerts: new List<Alert> { existing });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.Single(active);
        Assert.Equal(99, active[0].Id);
        mocks.AlertRepo.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task ExistingAlert_ClinicDefaultChangedSinceCreation_RefreshesToCurrentEffectiveUrgency()
    {
        // The row was created back when the clinic default was Red; the clinic
        // has since changed it to Yellow. The alert must reflect that now, not
        // stay frozen at whatever it was when first raised.
        var patient = MakePatient(1, batteryLevel: 5m);
        var existing = new Alert
        {
            Id = 99,
            PatientId = 1,
            TenantId = 1,
            AlertType = AlertType.LowBattery,
            Urgency = AlertUrgency.Red,
            TriggeredAt = DateTime.UtcNow.AddDays(-10),
            IsAcknowledged = false
        };
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Yellow),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        }, existingAlerts: new List<Alert> { existing });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        var alert = Assert.Single(active);
        Assert.Equal(99, alert.Id);
        Assert.Equal(AlertUrgency.Yellow, alert.Urgency);
        mocks.AlertRepo.Verify(r => r.UpdateAsync(It.Is<Alert>(a => a.Id == 99 && a.Urgency == AlertUrgency.Yellow)), Times.Once);
        mocks.AlertRepo.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task AcknowledgedAlert_NeverResurfaces_EvenThoughTheStaticConditionStillTriggers()
    {
        // Our trigger conditions come from data that never changes on its own
        // (simulated), so once a clinician has permanently acknowledged an alert,
        // a fresh one must not be manufactured on the very next evaluation.
        var patient = MakePatient(1, batteryLevel: 5m);
        var acknowledged = new Alert
        {
            Id = 99,
            PatientId = 1,
            TenantId = 1,
            AlertType = AlertType.LowBattery,
            Urgency = AlertUrgency.Red,
            TriggeredAt = DateTime.UtcNow.AddDays(-3),
            IsAcknowledged = true,
            AcknowledgedAt = DateTime.UtcNow.AddHours(-1)
        };
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Red),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        }, existingAlerts: new List<Alert> { acknowledged });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.DoesNotContain(active, a => a.AlertType == AlertType.LowBattery);
        mocks.AlertRepo.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task SnoozedAlert_IsSuppressedUntilSnoozeExpires()
    {
        var patient = MakePatient(1, batteryLevel: 5m);
        var snoozed = new Alert
        {
            Id = 99,
            PatientId = 1,
            TenantId = 1,
            AlertType = AlertType.LowBattery,
            Urgency = AlertUrgency.Red,
            TriggeredAt = DateTime.UtcNow.AddDays(-3),
            IsAcknowledged = false,
            SnoozedUntil = DateTime.UtcNow.AddDays(10)
        };
        var mocks = MakeMocks(1, new List<ClinicAlertSettings>
        {
            ClinicDefault(1, AlertType.LowBattery, AlertUrgency.Red),
            ClinicDefault(1, AlertType.DisconnectedMonitor, AlertUrgency.None),
            ClinicDefault(1, AlertType.IrregularHeartbeat, AlertUrgency.None)
        }, existingAlerts: new List<Alert> { snoozed });

        var active = await mocks.BuildService().GetActiveAlertsForPatientAsync(patient);

        Assert.DoesNotContain(active, a => a.AlertType == AlertType.LowBattery);
        mocks.AlertRepo.Verify(r => r.AddAsync(It.IsAny<Alert>()), Times.Never);
    }
}
