using System.Text;
using System.Text.Json;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Dashboard;
using CareLink.Application.Dashboard.Queries;
using CareLink.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace CareLink.UnitTests.Dashboard;

public class GetDashboardSummaryQueryHandlerCachingTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedValueAndNeverQueriesTheDatabase()
    {
        var cachedSummary = new DashboardSummaryDto(3, 1, 10, 2);
        var cachedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedSummary));

        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync($"dashboard-summary:1", It.IsAny<CancellationToken>())).ReturnsAsync(cachedBytes);

        var patientRepositoryMock = new Mock<IPatientRepository>();
        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();

        var handler = new GetDashboardSummaryQueryHandler(patientRepositoryMock.Object, alertEvaluationServiceMock.Object, cacheMock.Object);

        var result = await handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);

        Assert.Equal(cachedSummary, result);
        patientRepositoryMock.Verify(r => r.GetByTenantIdAsync(It.IsAny<int>()), Times.Never);
        alertEvaluationServiceMock.Verify(a => a.GetActiveAlertsForTenantAsync(It.IsAny<int>()), Times.Never);
        cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_ComputesFromDatabaseAndPopulatesCacheWithFreshValue()
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync($"dashboard-summary:1", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        var patientRepositoryMock = new Mock<IPatientRepository>();
        patientRepositoryMock.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(new List<Patient>());

        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();
        alertEvaluationServiceMock.Setup(a => a.GetActiveAlertsForTenantAsync(1)).ReturnsAsync(new List<Alert>());

        var handler = new GetDashboardSummaryQueryHandler(patientRepositoryMock.Object, alertEvaluationServiceMock.Object, cacheMock.Object);

        var result = await handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);

        Assert.Equal(0, result.NewPatientsCount);
        patientRepositoryMock.Verify(r => r.GetByTenantIdAsync(1), Times.Once);

        cacheMock.Verify(c => c.SetAsync(
            "dashboard-summary:1",
            It.Is<byte[]>(bytes => JsonSerializer.Deserialize<DashboardSummaryDto>(Encoding.UTF8.GetString(bytes)) == result),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(3)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentTenants_UseDistinctCacheKeys()
    {
        // The cache key must be tenant-scoped, otherwise one hospital's dashboard
        // counts could leak into another's.
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        var patientRepositoryMock = new Mock<IPatientRepository>();
        patientRepositoryMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<int>())).ReturnsAsync(new List<Patient>());

        var alertEvaluationServiceMock = new Mock<IAlertEvaluationService>();
        alertEvaluationServiceMock.Setup(a => a.GetActiveAlertsForTenantAsync(It.IsAny<int>())).ReturnsAsync(new List<Alert>());

        var handler = new GetDashboardSummaryQueryHandler(patientRepositoryMock.Object, alertEvaluationServiceMock.Object, cacheMock.Object);

        await handler.Handle(new GetDashboardSummaryQuery(1), CancellationToken.None);
        await handler.Handle(new GetDashboardSummaryQuery(2), CancellationToken.None);

        cacheMock.Verify(c => c.GetAsync("dashboard-summary:1", It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(c => c.GetAsync("dashboard-summary:2", It.IsAny<CancellationToken>()), Times.Once);
    }
}
