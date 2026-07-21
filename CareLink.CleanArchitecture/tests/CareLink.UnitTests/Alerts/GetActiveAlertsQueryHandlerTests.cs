using CareLink.Application.Alerts.Queries;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Alerts;

public class GetActiveAlertsQueryHandlerTests
{
    [Fact]
    public async Task Handle_TenantWithAlerts_ReturnsOnlyThatTenantsAlerts()
    {
        var tenant1Alerts = new List<Alert>
        {
            new() { Id = 1, PatientId = 1, TenantId = 1, AlertType = AlertType.LowBattery, Urgency = AlertUrgency.Red },
            new() { Id = 2, PatientId = 2, TenantId = 1, AlertType = AlertType.DisconnectedMonitor, Urgency = AlertUrgency.Red }
        };

        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();
        alertEvaluationServiceMock.Setup(s => s.GetActiveAlertsForTenantAsync(1)).ReturnsAsync(tenant1Alerts);

        var handler = new GetActiveAlertsQueryHandler(alertEvaluationServiceMock.Object);

        var result = await handler.Handle(new GetActiveAlertsQuery(1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(1, dto.TenantId));
        alertEvaluationServiceMock.Verify(s => s.GetActiveAlertsForTenantAsync(1), Times.Once);
        alertEvaluationServiceMock.Verify(s => s.GetActiveAlertsForTenantAsync(It.Is<int>(id => id != 1)), Times.Never);
    }

    [Fact]
    public async Task Handle_TenantWithNoAlerts_ReturnsEmptyList()
    {
        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();
        alertEvaluationServiceMock.Setup(s => s.GetActiveAlertsForTenantAsync(99)).ReturnsAsync(new List<Alert>());

        var handler = new GetActiveAlertsQueryHandler(alertEvaluationServiceMock.Object);

        var result = await handler.Handle(new GetActiveAlertsQuery(99), CancellationToken.None);

        Assert.Empty(result);
    }
}
