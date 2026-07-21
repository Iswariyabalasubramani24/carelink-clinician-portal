using CareLink.Application.AuditLogs.Queries;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.AuditLogs;

public class GetAuditLogsQueryHandlerTests
{
    private static AuditLog MakeEntry(int id, int tenantId, string action) => new()
    {
        Id = id,
        ClinicianId = 1,
        TenantId = tenantId,
        Action = action,
        EntityType = "Patient",
        EntityId = id,
        Timestamp = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_OnlyRequestsEntriesForTheGivenTenantId()
    {
        // The repository call is tenant-scoped by construction (it only
        // accepts a single tenantId), so an admin can never receive another
        // hospital's audit trail back from this query.
        var tenant1Entries = new List<AuditLog>
        {
            MakeEntry(1, tenantId: 1, "PatientCreated"),
            MakeEntry(2, tenantId: 1, "AlertAcknowledged")
        };

        var repoMock = new Mock<IAuditLogRepository>();
        repoMock.Setup(r => r.GetByTenantIdAsync(1, null, 1, 25)).ReturnsAsync((tenant1Entries, 2));

        var handler = new GetAuditLogsQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetAuditLogsQuery(1), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        repoMock.Verify(r => r.GetByTenantIdAsync(2, It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PageSizeAboveMax_IsClampedBeforeCallingRepository()
    {
        var repoMock = new Mock<IAuditLogRepository>();
        repoMock.Setup(r => r.GetByTenantIdAsync(1, null, 1, 100)).ReturnsAsync((new List<AuditLog>(), 0));

        var handler = new GetAuditLogsQueryHandler(repoMock.Object);

        await handler.Handle(new GetAuditLogsQuery(1, page: 1, pageSize: 500), CancellationToken.None);

        repoMock.Verify(r => r.GetByTenantIdAsync(1, null, 1, 100), Times.Once);
    }
}
