using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Hospitals.Commands;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CareLink.UnitTests.Hospitals;

// Note: [Authorize(Roles="SuperAdmin")] on HospitalsController is what rejects
// non-super-admins with 403 - that's ASP.NET Core's authorization middleware,
// not handler logic, so it isn't re-verified here. What IS verified: the whole
// hospital graph (tenant + admin + access row + default settings) is staged
// and committed through a single SaveChangesAsync, and the temporary password
// is returned in plaintext exactly once while only its hash is persisted.
public class ProvisionHospitalCommandHandlerTests
{
    private readonly Mock<DbSet<Tenant>> _tenants = new();
    private readonly Mock<DbSet<Clinician>> _clinicians = new();
    private readonly Mock<DbSet<ClinicianTenant>> _clinicianTenants = new();
    private readonly Mock<DbSet<ClinicAlertSettings>> _alertSettings = new();
    private readonly Mock<DbSet<PatientScheduleSettings>> _scheduleSettings = new();
    private readonly Mock<DbSet<ReportSettings>> _reportSettings = new();
    private readonly Mock<IApplicationDbContext> _db = new();
    private readonly Mock<IClinicianRepository> _clinicianRepo = new();
    private readonly Mock<IAuditLogger> _auditLogger = new();

    private readonly List<Tenant> _addedTenants = [];
    private readonly List<Clinician> _addedClinicians = [];
    private readonly List<ClinicianTenant> _addedAccessRows = [];
    private readonly List<ClinicAlertSettings> _addedAlertSettings = [];
    private readonly List<PatientScheduleSettings> _addedScheduleSettings = [];
    private readonly List<ReportSettings> _addedReportSettings = [];
    private int _saveCalls;

    private static ProvisionHospitalCommand ValidCommand() => new()
    {
        ActingClinicianId = 99,
        Name = "Nihon Medical Center",
        Region = "Japan",
        LanguageCode = "en",
        AdminFirstName = "Kenji",
        AdminLastName = "Tanaka",
        AdminEmail = "admin@nihonmedical.jp"
    };

    private ProvisionHospitalCommandHandler CreateHandler(string tempPassword = "Tmp#Passw0rd")
    {
        _tenants.Setup(s => s.Add(It.IsAny<Tenant>())).Callback<Tenant>(_addedTenants.Add);
        _clinicians.Setup(s => s.Add(It.IsAny<Clinician>())).Callback<Clinician>(_addedClinicians.Add);
        _clinicianTenants.Setup(s => s.Add(It.IsAny<ClinicianTenant>())).Callback<ClinicianTenant>(_addedAccessRows.Add);
        _alertSettings.Setup(s => s.Add(It.IsAny<ClinicAlertSettings>())).Callback<ClinicAlertSettings>(_addedAlertSettings.Add);
        _scheduleSettings.Setup(s => s.Add(It.IsAny<PatientScheduleSettings>())).Callback<PatientScheduleSettings>(_addedScheduleSettings.Add);
        _reportSettings.Setup(s => s.Add(It.IsAny<ReportSettings>())).Callback<ReportSettings>(_addedReportSettings.Add);

        _db.SetupGet(d => d.Tenants).Returns(_tenants.Object);
        _db.SetupGet(d => d.Clinicians).Returns(_clinicians.Object);
        _db.SetupGet(d => d.ClinicianTenants).Returns(_clinicianTenants.Object);
        _db.SetupGet(d => d.ClinicAlertSettings).Returns(_alertSettings.Object);
        _db.SetupGet(d => d.PatientScheduleSettings).Returns(_scheduleSettings.Object);
        _db.SetupGet(d => d.ReportSettings).Returns(_reportSettings.Object);

        // Simulate EF assigning identity keys at commit time.
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _saveCalls++;
                foreach (var t in _addedTenants) t.Id = 500;
                foreach (var c in _addedClinicians) c.Id = 600;
            })
            .ReturnsAsync(1);

        _clinicianRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Clinician?)null);

        var tempPasswordGenerator = new Mock<ITemporaryPasswordGenerator>();
        tempPasswordGenerator.Setup(g => g.Generate()).Returns(tempPassword);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Hash(tempPassword)).Returns("hashed-temp-password");

        return new ProvisionHospitalCommandHandler(
            _db.Object, _clinicianRepo.Object, tempPasswordGenerator.Object, passwordHasher.Object, _auditLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesTenantWithFirstAdminAndReturnsPlaintextTempPassword()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("Tmp#Passw0rd", result.TemporaryPassword);
        Assert.Equal("admin@nihonmedical.jp", result.AdminEmail);
        Assert.Equal(500, result.Hospital.Id);
        Assert.Equal("Nihon Medical Center", result.Hospital.Name);
        Assert.Equal("Japan", result.Hospital.Region);
        Assert.True(result.Hospital.IsActive);
        Assert.Equal(1, result.Hospital.ClinicianCount);

        var tenant = Assert.Single(_addedTenants);
        Assert.False(tenant.IsSystem);

        var admin = Assert.Single(_addedClinicians);
        Assert.Equal(ClinicianRole.Admin, admin.Role);
        Assert.Same(tenant, admin.Tenant);
        Assert.Equal("hashed-temp-password", admin.PasswordHash);
        Assert.NotEqual("Tmp#Passw0rd", admin.PasswordHash);
        Assert.True(admin.IsActive);

        var access = Assert.Single(_addedAccessRows);
        Assert.Same(admin, access.Clinician);
        Assert.Same(tenant, access.Tenant);
    }

    [Fact]
    public async Task Handle_ValidRequest_ProvisionsAllClinicDefaultsInOneAtomicSave()
    {
        var handler = CreateHandler();

        await handler.Handle(ValidCommand(), CancellationToken.None);

        // One alert default per alert type, each anchored to the new tenant.
        Assert.Equal(3, _addedAlertSettings.Count);
        Assert.Contains(_addedAlertSettings, s => s.AlertType == AlertType.DisconnectedMonitor && s.DefaultUrgency == AlertUrgency.Red);
        Assert.Contains(_addedAlertSettings, s => s.AlertType == AlertType.LowBattery && s.DefaultUrgency == AlertUrgency.Yellow);
        Assert.Contains(_addedAlertSettings, s => s.AlertType == AlertType.IrregularHeartbeat && s.DefaultUrgency == AlertUrgency.Yellow);
        Assert.All(_addedAlertSettings, s => Assert.Same(_addedTenants[0], s.Tenant));

        var schedule = Assert.Single(_addedScheduleSettings);
        Assert.Equal(30, schedule.IntervalDays);
        Assert.Null(schedule.PatientId);

        var report = Assert.Single(_addedReportSettings);
        Assert.Equal(30, report.IntervalDays);
        Assert.Null(report.PatientId);

        // The whole graph must commit atomically - exactly one SaveChangesAsync.
        Assert.Equal(1, _saveCalls);
    }

    [Fact]
    public async Task Handle_ValidRequest_WritesAuditEntryWithoutTheTemporaryPassword()
    {
        var handler = CreateHandler(tempPassword: "Secret#Temp99");

        await handler.Handle(ValidCommand(), CancellationToken.None);

        _auditLogger.Verify(a => a.LogAsync(
            99, 500, "TenantProvisioned", "Tenant", 500,
            It.Is<string?>(d => d != null && d.Contains("Nihon Medical Center") && !d.Contains("Secret#Temp99"))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AdminEmailAlreadyExists_ThrowsAndProvisionsNothing()
    {
        var handler = CreateHandler();
        _clinicianRepo.Setup(r => r.GetByEmailAsync("admin@nihonmedical.jp"))
            .ReturnsAsync(new Clinician { Id = 1, Email = "admin@nihonmedical.jp" });

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Empty(_addedTenants);
        Assert.Empty(_addedClinicians);
        Assert.Equal(0, _saveCalls);
    }
}
