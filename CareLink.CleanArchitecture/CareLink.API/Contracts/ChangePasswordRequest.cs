namespace CareLink.API.Contracts;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
