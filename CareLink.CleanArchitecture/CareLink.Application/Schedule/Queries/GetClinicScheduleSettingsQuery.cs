using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Schedule.Queries;

public class GetClinicScheduleSettingsQuery : IRequest<ScheduleSettingsDto>
{
    public int TenantId { get; set; }

    public GetClinicScheduleSettingsQuery() { }

    public GetClinicScheduleSettingsQuery(int tenantId)
    {
        TenantId = tenantId;
    }
}

public class GetClinicScheduleSettingsQueryHandler(IScheduleSettingsRepository scheduleSettingsRepository)
    : IRequestHandler<GetClinicScheduleSettingsQuery, ScheduleSettingsDto>
{
    public const int DefaultIntervalDays = 30;

    public async Task<ScheduleSettingsDto> Handle(GetClinicScheduleSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await scheduleSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return new ScheduleSettingsDto(settings?.IntervalDays ?? DefaultIntervalDays);
    }
}
