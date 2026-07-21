using CareLink.Application.Alerts;

namespace CareLink.API.Contracts;

public record UpdateClinicAlertSettingsRequest(List<ClinicAlertSettingsDto> Settings);
