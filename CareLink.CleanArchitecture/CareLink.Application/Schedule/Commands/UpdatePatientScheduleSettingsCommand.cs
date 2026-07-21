using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Schedule.Queries;
using MediatR;

namespace CareLink.Application.Schedule.Commands;

public class UpdatePatientScheduleSettingsCommand : IRequest<PatientScheduleSettingsDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    // false = revert to the clinic-wide default interval (the override row is removed).
    public bool UseOverride { get; set; }

    public int IntervalDays { get; set; }
}

public class UpdatePatientScheduleSettingsCommandHandler(
    IPatientRepository patientRepository,
    IScheduleSettingsRepository scheduleSettingsRepository)
    : IRequestHandler<UpdatePatientScheduleSettingsCommand, PatientScheduleSettingsDto>
{
    public async Task<PatientScheduleSettingsDto> Handle(UpdatePatientScheduleSettingsCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        if (request.UseOverride)
        {
            await scheduleSettingsRepository.UpsertPatientOverrideAsync(request.PatientId, request.IntervalDays);
            return new PatientScheduleSettingsDto(request.IntervalDays, IsOverride: true);
        }

        await scheduleSettingsRepository.RemovePatientOverrideAsync(request.PatientId);
        var clinicSettings = await scheduleSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return new PatientScheduleSettingsDto(
            clinicSettings?.IntervalDays ?? GetClinicScheduleSettingsQueryHandler.DefaultIntervalDays,
            IsOverride: false);
    }
}
