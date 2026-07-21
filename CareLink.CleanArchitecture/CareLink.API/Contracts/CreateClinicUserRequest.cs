using CareLink.Domain.Entities;

namespace CareLink.API.Contracts;

public record CreateClinicUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string LanguageCode,
    ClinicianRole Role = ClinicianRole.Clinician);
