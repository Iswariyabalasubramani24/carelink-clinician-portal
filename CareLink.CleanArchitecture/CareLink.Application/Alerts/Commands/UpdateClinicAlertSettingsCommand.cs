using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Alerts.Commands;

public class UpdateClinicAlertSettingsCommand : IRequest<List<ClinicAlertSettingsDto>>
{
    public int TenantId { get; set; }

    public List<ClinicAlertSettingsDto> Settings { get; set; } = [];
}

public class UpdateClinicAlertSettingsCommandHandler(IClinicAlertSettingsRepository clinicAlertSettingsRepository)
    : IRequestHandler<UpdateClinicAlertSettingsCommand, List<ClinicAlertSettingsDto>>
{
    public async Task<List<ClinicAlertSettingsDto>> Handle(UpdateClinicAlertSettingsCommand request, CancellationToken cancellationToken)
    {
        foreach (var setting in request.Settings)
        {
            await clinicAlertSettingsRepository.UpsertAsync(request.TenantId, setting.AlertType, setting.DefaultUrgency);
        }

        var updated = await clinicAlertSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return updated.Select(s => new ClinicAlertSettingsDto(s.AlertType, s.DefaultUrgency)).ToList();
    }
}
