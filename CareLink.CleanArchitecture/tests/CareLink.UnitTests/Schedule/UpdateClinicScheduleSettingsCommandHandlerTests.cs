using CareLink.Application.Common.Interfaces;
using CareLink.Application.Schedule.Commands;
using Moq;

namespace CareLink.UnitTests.Schedule;

public class UpdateClinicScheduleSettingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_UpsertsTenantSettingsAndReturnsNewInterval()
    {
        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        var handler = new UpdateClinicScheduleSettingsCommandHandler(scheduleRepo.Object);

        var result = await handler.Handle(
            new UpdateClinicScheduleSettingsCommand { TenantId = 1, IntervalDays = 45 },
            CancellationToken.None);

        Assert.Equal(45, result.IntervalDays);
        scheduleRepo.Verify(r => r.UpsertTenantSettingsAsync(1, 45), Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentTenants_UpsertOnlyTheirOwnTenantId()
    {
        var scheduleRepo = new Mock<IScheduleSettingsRepository>();
        var handler = new UpdateClinicScheduleSettingsCommandHandler(scheduleRepo.Object);

        await handler.Handle(new UpdateClinicScheduleSettingsCommand { TenantId = 2, IntervalDays = 15 }, CancellationToken.None);

        scheduleRepo.Verify(r => r.UpsertTenantSettingsAsync(2, 15), Times.Once);
        scheduleRepo.Verify(r => r.UpsertTenantSettingsAsync(1, It.IsAny<int>()), Times.Never);
    }
}
