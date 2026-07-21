using CareLink.Application.Common.Interfaces;
using CareLink.Application.Schedule.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Schedule;

public class GetClinicScheduleSettingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_TenantHasConfiguredSettings_ReturnsConfiguredInterval()
    {
        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 21 });

        var handler = new GetClinicScheduleSettingsQueryHandler(scheduleRepo.Object);

        var result = await handler.Handle(new GetClinicScheduleSettingsQuery(1), CancellationToken.None);

        Assert.Equal(21, result.IntervalDays);
    }

    [Fact]
    public async Task Handle_NoSettingsConfiguredForTenant_ReturnsHardcodedDefault()
    {
        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync((PatientScheduleSettings?)null);

        var handler = new GetClinicScheduleSettingsQueryHandler(scheduleRepo.Object);

        var result = await handler.Handle(new GetClinicScheduleSettingsQuery(1), CancellationToken.None);

        Assert.Equal(30, result.IntervalDays);
    }

    [Fact]
    public async Task Handle_DifferentTenants_QueryOnlyTheirOwnTenantId()
    {
        // Tenant isolation: tenant 2's clinic-wide interval must never be
        // influenced by, or leak from, tenant 1's configured settings.
        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 1, IntervalDays = 21 });
        scheduleRepo.Setup(r => r.GetByTenantIdAsync(2))
            .ReturnsAsync(new PatientScheduleSettings { TenantId = 2, IntervalDays = 60 });

        var handler = new GetClinicScheduleSettingsQueryHandler(scheduleRepo.Object);

        var tenant1Result = await handler.Handle(new GetClinicScheduleSettingsQuery(1), CancellationToken.None);
        var tenant2Result = await handler.Handle(new GetClinicScheduleSettingsQuery(2), CancellationToken.None);

        Assert.Equal(21, tenant1Result.IntervalDays);
        Assert.Equal(60, tenant2Result.IntervalDays);
    }
}
