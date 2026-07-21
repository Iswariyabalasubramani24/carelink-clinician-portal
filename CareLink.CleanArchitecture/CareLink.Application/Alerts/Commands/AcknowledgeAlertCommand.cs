using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

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

    public AlertAcknowledgeAction Action { get; set; }
}

public class AcknowledgeAlertCommandHandler(IAlertRepository alertRepository)
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

        return AlertDto.FromEntity(alert);
    }
}
