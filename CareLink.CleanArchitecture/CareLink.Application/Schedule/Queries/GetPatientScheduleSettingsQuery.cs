using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Schedule.Queries;

public class GetPatientScheduleSettingsQuery : IRequest<PatientScheduleSettingsDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientScheduleSettingsQuery() { }

    public GetPatientScheduleSettingsQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientScheduleSettingsQueryHandler(
    IPatientRepository patientRepository,
    IScheduleSettingsRepository scheduleSettingsRepository)
    : IRequestHandler<GetPatientScheduleSettingsQuery, PatientScheduleSettingsDto>
{
    public async Task<PatientScheduleSettingsDto> Handle(GetPatientScheduleSettingsQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var patientOverride = await scheduleSettingsRepository.GetByPatientIdAsync(request.PatientId);
        if (patientOverride is not null)
        {
            return new PatientScheduleSettingsDto(patientOverride.IntervalDays, IsOverride: true);
        }

        var clinicSettings = await scheduleSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return new PatientScheduleSettingsDto(
            clinicSettings?.IntervalDays ?? GetClinicScheduleSettingsQueryHandler.DefaultIntervalDays,
            IsOverride: false);
    }
}
