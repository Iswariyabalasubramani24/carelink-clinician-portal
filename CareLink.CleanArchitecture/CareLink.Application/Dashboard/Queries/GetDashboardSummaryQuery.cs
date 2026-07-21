using CareLink.Application.Common.Interfaces;
using MediatR;

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

public class GetDashboardSummaryQueryHandler(IPatientRepository patientRepository, IAlertEvaluationService alertEvaluationService)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private const int NewPatientWindowDays = 7;
    private const int DisconnectedThresholdDays = 7;

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var patients = await patientRepository.GetByTenantIdAsync(request.TenantId);
        var activePatients = patients.Where(p => p.IsActive).ToList();

        var now = DateTime.UtcNow;
        var newPatientsCutoff = now.AddDays(-NewPatientWindowDays);
        var disconnectedCutoff = now.AddDays(-DisconnectedThresholdDays);

        var newPatientsCount = activePatients.Count(p => p.CreatedAt >= newPatientsCutoff);
        var disconnectedMonitorsCount = activePatients.Count(p => p.LastSyncedAt is null || p.LastSyncedAt < disconnectedCutoff);
        var totalActivePatientsCount = activePatients.Count;

        var activeAlerts = await alertEvaluationService.GetActiveAlertsForTenantAsync(request.TenantId);

        return new DashboardSummaryDto(newPatientsCount, disconnectedMonitorsCount, totalActivePatientsCount, activeAlerts.Count);
    }
}
