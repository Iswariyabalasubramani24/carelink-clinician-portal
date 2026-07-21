using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Application.Reports.Queries;
using MediatR;

namespace CareLink.Application.Reports.Commands;

public class UpdatePatientReportSettingsCommand : IRequest<PatientReportSettingsDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    // false = revert to the clinic-wide default interval (the override row is removed).
    public bool UseOverride { get; set; }

    public int IntervalDays { get; set; }
}

public class UpdatePatientReportSettingsCommandHandler(
    IPatientRepository patientRepository,
    IReportSettingsRepository reportSettingsRepository)
    : IRequestHandler<UpdatePatientReportSettingsCommand, PatientReportSettingsDto>
{
    public async Task<PatientReportSettingsDto> Handle(UpdatePatientReportSettingsCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        if (request.UseOverride)
        {
            await reportSettingsRepository.UpsertPatientOverrideAsync(request.PatientId, request.IntervalDays);
            return new PatientReportSettingsDto(request.IntervalDays, IsOverride: true);
        }

        await reportSettingsRepository.RemovePatientOverrideAsync(request.PatientId);
        var clinicSettings = await reportSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return new PatientReportSettingsDto(
            clinicSettings?.IntervalDays ?? GetClinicReportSettingsQueryHandler.DefaultIntervalDays,
            IsOverride: false);
    }
}
