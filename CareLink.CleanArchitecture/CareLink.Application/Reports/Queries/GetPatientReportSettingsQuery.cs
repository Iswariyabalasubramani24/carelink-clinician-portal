using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Reports.Queries;

public class GetPatientReportSettingsQuery : IRequest<PatientReportSettingsDto>
{
    public int PatientId { get; set; }

    public int TenantId { get; set; }

    public GetPatientReportSettingsQuery() { }

    public GetPatientReportSettingsQuery(int patientId, int tenantId)
    {
        PatientId = patientId;
        TenantId = tenantId;
    }
}

public class GetPatientReportSettingsQueryHandler(
    IPatientRepository patientRepository,
    IReportSettingsRepository reportSettingsRepository)
    : IRequestHandler<GetPatientReportSettingsQuery, PatientReportSettingsDto>
{
    public async Task<PatientReportSettingsDto> Handle(GetPatientReportSettingsQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, request.TenantId);
        if (patient is null)
        {
            throw new PatientNotFoundException();
        }

        var patientOverride = await reportSettingsRepository.GetByPatientIdAsync(request.PatientId);
        if (patientOverride is not null)
        {
            return new PatientReportSettingsDto(patientOverride.IntervalDays, IsOverride: true);
        }

        var clinicSettings = await reportSettingsRepository.GetByTenantIdAsync(request.TenantId);
        return new PatientReportSettingsDto(
            clinicSettings?.IntervalDays ?? GetClinicReportSettingsQueryHandler.DefaultIntervalDays,
            IsOverride: false);
    }
}
