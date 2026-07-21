using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Reports.Queries;

public class GetClinicReportSettingsQuery : IRequest<ReportSettingsDto>
{
    public int TenantId { get; set; }

    public GetClinicReportSettingsQuery() { }

    public GetClinicReportSettingsQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetClinicReportSettingsQueryHandler(IReportSettingsRepository reportSettingsRepository)
    : IRequestHandler<GetClinicReportSettingsQuery, ReportSettingsDto>
{
    public const int DefaultIntervalDays = 30;

    public async Task<ReportSettingsDto> Handle(GetClinicReportSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await reportSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return new ReportSettingsDto(settings?.IntervalDays ?? DefaultIntervalDays);
    }
}
