using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Reports.Commands;

public class UpdateClinicReportSettingsCommand : IRequest<ReportSettingsDto>
{
    public int TenantId { get; set; }

    public int IntervalDays { get; set; }
}

public class UpdateClinicReportSettingsCommandHandler(IReportSettingsRepository reportSettingsRepository)
    : IRequestHandler<UpdateClinicReportSettingsCommand, ReportSettingsDto>
{
    public async Task<ReportSettingsDto> Handle(UpdateClinicReportSettingsCommand request, CancellationToken cancellationToken)
    {
        await reportSettingsRepository.UpsertTenantSettingsAsync(request.TenantId, request.IntervalDays);
        return new ReportSettingsDto(request.IntervalDays);
    }
}
