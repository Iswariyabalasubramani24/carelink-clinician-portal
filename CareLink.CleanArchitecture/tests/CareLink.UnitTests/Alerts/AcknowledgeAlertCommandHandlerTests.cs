using CareLink.Application.Alerts.Commands;
using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Alerts;

public class AcknowledgeAlertCommandHandlerTests
{
    private static Alert MakeAlert(int id = 1, int tenantId = 1) => new()
    {
        Id = id,
        PatientId = 1,
        TenantId = tenantId,
        AlertType = AlertType.LowBattery,
        Urgency = AlertUrgency.Red,
        TriggeredAt = DateTime.UtcNow.AddDays(-1),
        IsAcknowledged = false
    };

    [Fact]
    public async Task Handle_AcknowledgeAction_MarksAlertPermanentlyAcknowledged()
    {
        var alert = MakeAlert();

        var repositoryMock = new Mock<IAlertRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(alert);

        var handler = new AcknowledgeAlertCommandHandler(repositoryMock.Object);

        var result = await handler.Handle(
            new AcknowledgeAlertCommand { AlertId = 1, TenantId = 1, Action = AlertAcknowledgeAction.Acknowledge },
            CancellationToken.None);

        Assert.True(result.IsAcknowledged);
        Assert.NotNull(result.AcknowledgedAt);
        Assert.Null(result.SnoozedUntil);
        repositoryMock.Verify(r => r.UpdateAsync(It.Is<Alert>(a => a.IsAcknowledged && a.AcknowledgedAt != null)), Times.Once);
    }

    [Fact]
    public async Task Handle_SnoozeAction_SetsSnoozedUntilFifteenDaysOutWithoutAcknowledging()
    {
        var alert = MakeAlert();

        var repositoryMock = new Mock<IAlertRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(alert);

        var handler = new AcknowledgeAlertCommandHandler(repositoryMock.Object);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(
            new AcknowledgeAlertCommand { AlertId = 1, TenantId = 1, Action = AlertAcknowledgeAction.Snooze },
            CancellationToken.None);

        Assert.False(result.IsAcknowledged);
        Assert.Null(result.AcknowledgedAt);
        Assert.NotNull(result.SnoozedUntil);
        Assert.InRange(result.SnoozedUntil!.Value, before.AddDays(15).AddMinutes(-1), before.AddDays(15).AddMinutes(1));
        repositoryMock.Verify(r => r.UpdateAsync(It.Is<Alert>(a => !a.IsAcknowledged && a.SnoozedUntil != null)), Times.Once);
    }

    [Fact]
    public async Task Handle_AlertNotFoundForTenant_ThrowsAlertNotFoundException()
    {
        var repositoryMock = new Mock<IAlertRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync((Alert?)null);

        var handler = new AcknowledgeAlertCommandHandler(repositoryMock.Object);

        await Assert.ThrowsAsync<AlertNotFoundException>(() => handler.Handle(
            new AcknowledgeAlertCommand { AlertId = 1, TenantId = 1, Action = AlertAcknowledgeAction.Acknowledge },
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlertBelongsToDifferentTenant_ThrowsAlertNotFoundException()
    {
        // The repository is tenant-scoped, so an alert that exists but belongs to a
        // different tenant must come back as "not found" here, never leaked or acted on.
        var repositoryMock = new Mock<IAlertRepository>();
        repositoryMock.Setup(r => r.GetByIdAsync(1, 2)).ReturnsAsync((Alert?)null);

        var handler = new AcknowledgeAlertCommandHandler(repositoryMock.Object);

        await Assert.ThrowsAsync<AlertNotFoundException>(() => handler.Handle(
            new AcknowledgeAlertCommand { AlertId = 1, TenantId = 2, Action = AlertAcknowledgeAction.Acknowledge },
            CancellationToken.None));

        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Alert>()), Times.Never);
    }
}
