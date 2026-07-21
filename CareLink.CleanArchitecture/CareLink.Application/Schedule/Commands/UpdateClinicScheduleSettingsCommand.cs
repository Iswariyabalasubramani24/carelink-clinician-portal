using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Schedule.Commands;

public class UpdateClinicScheduleSettingsCommand : IRequest<ScheduleSettingsDto>
{
    public int TenantId { get; set; }

    public int IntervalDays { get; set; }
}

public class UpdateClinicScheduleSettingsCommandHandler(IScheduleSettingsRepository scheduleSettingsRepository)
    : IRequestHandler<UpdateClinicScheduleSettingsCommand, ScheduleSettingsDto>
{
    public async Task<ScheduleSettingsDto> Handle(UpdateClinicScheduleSettingsCommand request, CancellationToken cancellationToken)
    {
        await scheduleSettingsRepository.UpsertTenantSettingsAsync(request.TenantId, request.IntervalDays);
        return new ScheduleSettingsDto(request.IntervalDays);
    }
}
