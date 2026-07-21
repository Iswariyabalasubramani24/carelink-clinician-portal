using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Alerts.Queries;

public class GetActiveAlertsQuery : IRequest<List<AlertDto>>
{
    public int TenantId { get; set; }

    public GetActiveAlertsQuery() { }

    public GetActiveAlertsQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetActiveAlertsQueryHandler(IAlertEvaluationService alertEvaluationService)
    : IRequestHandler<GetActiveAlertsQuery, List<AlertDto>>
{
    public async Task<List<AlertDto>> Handle(GetActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        var alerts = await alertEvaluationService.GetActiveAlertsForTenantAsync(request.TenantId);
        return alerts.Select(AlertDto.FromEntity).ToList();
    }
}
