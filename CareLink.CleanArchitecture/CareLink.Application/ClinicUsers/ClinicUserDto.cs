using CareLink.Domain.Entities;

namespace CareLink.Application.ClinicUsers;

public record ClinicUserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string LanguageCode,
    bool IsActive,
    DateTime CreatedAt)
{
    public static ClinicUserDto FromEntity(Clinician c) => new(
        c.Id,
        c.FirstName,
        c.LastName,
        c.Email,
        c.Role.ToString(),
        c.LanguageCode,
        c.IsActive,
        c.CreatedAt);
}

public record CreateClinicUserResultDto(ClinicUserDto User, string TemporaryPassword);
