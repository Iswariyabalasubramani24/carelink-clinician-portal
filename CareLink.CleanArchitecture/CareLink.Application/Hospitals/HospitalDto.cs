using CareLink.Domain.Entities;

namespace CareLink.Application.Hospitals;

// Read model for the super-admin hospital-management list. Distinct from the
// public TenantDto (used by the login picker): it exposes management-only
// fields such as active status and how many clinician accounts exist.
public record HospitalDto(
    int Id,
    string Name,
    string Region,
    string LanguageCode,
    bool IsActive,
    int ClinicianCount);

public record ProvisionHospitalResultDto(HospitalDto Hospital, string AdminEmail, string TemporaryPassword);
