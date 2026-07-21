using System.Text.Json;
using CareLink.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace CareLink.Application.Dashboard.Queries;

public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
{
    public int TenantId { get; set; }

    public GetDashboardSummaryQuery() { }

    public GetDashboardSummaryQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetDashboardSummaryQueryHandler(
    IPatientRepository patientRepository, IAlertEvaluationService alertEvaluationService, IDistributedCache cache)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private const int NewPatientWindowDays = 7;
    private const int DisconnectedThresholdDays = 7;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);

    public static string CacheKey(int tenantId) => $"dashboard-summary:{tenantId}";

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(request.TenantId);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<DashboardSummaryDto>(cached)!;
        }

        var patients = await patientRepository.GetByTenantIdAsync(request.TenantId);
        var activePatients = patients.Where(p => p.IsActive).ToList();

        var now = DateTime.UtcNow;
        var newPatientsCutoff = now.AddDays(-NewPatientWindowDays);
        var disconnectedCutoff = now.AddDays(-DisconnectedThresholdDays);

        var newPatientsCount = activePatients.Count(p => p.CreatedAt >= newPatientsCutoff);
        var disconnectedMonitorsCount = activePatients.Count(p => p.LastSyncedAt is null || p.LastSyncedAt < disconnectedCutoff);
        var totalActivePatientsCount = activePatients.Count;

        var activeAlerts = await alertEvaluationService.GetActiveAlertsForTenantAsync(request.TenantId);

        var summary = new DashboardSummaryDto(newPatientsCount, disconnectedMonitorsCount, totalActivePatientsCount, activeAlerts.Count);

        await cache.SetStringAsync(
            cacheKey, JsonSerializer.Serialize(summary),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return summary;
    }
}
