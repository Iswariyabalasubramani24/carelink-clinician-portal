using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Dashboard.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace CareLink.Application.Alerts.Commands;

public enum AlertAcknowledgeAction
{
    Acknowledge,
    Snooze
}

public class AcknowledgeAlertCommand : IRequest<AlertDto>
{
    public int AlertId { get; set; }

    public int TenantId { get; set; }

    public int ClinicianId { get; set; }

    public AlertAcknowledgeAction Action { get; set; }
}

public class AcknowledgeAlertCommandHandler(IAlertRepository alertRepository, IAuditLogger auditLogger, IDistributedCache cache)
    : IRequestHandler<AcknowledgeAlertCommand, AlertDto>
{
    private const int SnoozeDays = 15;

    public async Task<AlertDto> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await alertRepository.GetByIdAsync(request.AlertId, request.TenantId);
        if (alert is null)
        {
            throw new AlertNotFoundException();
        }

        if (request.Action == AlertAcknowledgeAction.Acknowledge)
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
        }
        else
        {
            alert.SnoozedUntil = DateTime.UtcNow.AddDays(SnoozeDays);
        }

        await alertRepository.UpdateAsync(alert);

        var auditAction = request.Action == AlertAcknowledgeAction.Acknowledge ? "AlertAcknowledged" : "AlertSnoozed";
        await auditLogger.LogAsync(
            request.ClinicianId, request.TenantId, auditAction, "Alert", alert.Id,
            $"{alert.AlertType} for patient {alert.PatientId}");

        // The dashboard's Active Alerts count is now stale - drop it rather than
        // waiting out the TTL, so the next dashboard load recomputes it fresh.
        await cache.RemoveAsync(GetDashboardSummaryQueryHandler.CacheKey(request.TenantId), cancellationToken);

        return AlertDto.FromEntity(alert);
    }
}
