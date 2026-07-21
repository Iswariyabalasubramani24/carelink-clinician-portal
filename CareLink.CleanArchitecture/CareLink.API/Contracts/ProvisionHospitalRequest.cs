namespace CareLink.API.Contracts;

public record ProvisionHospitalRequest(
    string Name,
    string Region,
    string LanguageCode,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail);
