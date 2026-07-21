using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Alerts.Queries;

public class GetClinicAlertSettingsQuery : IRequest<List<ClinicAlertSettingsDto>>
{
    public int TenantId { get; set; }

    public GetClinicAlertSettingsQuery() { }

    public GetClinicAlertSettingsQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetClinicAlertSettingsQueryHandler(IClinicAlertSettingsRepository clinicAlertSettingsRepository)
    : IRequestHandler<GetClinicAlertSettingsQuery, List<ClinicAlertSettingsDto>>
{
    public async Task<List<ClinicAlertSettingsDto>> Handle(GetClinicAlertSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await clinicAlertSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return settings.Select(s => new ClinicAlertSettingsDto(s.AlertType, s.DefaultUrgency)).ToList();
    }
}
