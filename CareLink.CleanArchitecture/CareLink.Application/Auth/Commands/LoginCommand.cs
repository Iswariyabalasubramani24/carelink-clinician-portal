using CareLink.Application.Common.Exceptions;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using MediatR;

namespace CareLink.Application.Auth.Commands;

public class LoginCommand : IRequest<AuthResultDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginCommandHandler(
    IClinicianRepository clinicianRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var clinician = await clinicianRepository.GetByEmailAsync(request.Email);

        // Check credentials before suspension status: revealing "this account is
        // suspended" only to someone who already proved they know the correct
        // password avoids leaking account state to a credential-guessing attacker.
        if (clinician is null || !passwordHasher.Verify(request.Password, clinician.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (!clinician.IsActive)
        {
            throw new AccountSuspendedException();
        }

        var accessToken = tokenGenerator.GenerateAccessToken(clinician, clinician.TenantId);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            ClinicianId = clinician.Id,
            TenantId = clinician.TenantId,
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            IsRevoked = false
        });

        return new AuthResultDto(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            clinician.Id,
            clinician.Email,
            clinician.FirstName,
            clinician.LastName,
            clinician.Role.ToString(),
            clinician.TenantId);
    }
}
