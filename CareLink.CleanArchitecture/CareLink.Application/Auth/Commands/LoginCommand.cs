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

        if (clinician is null || !clinician.IsActive || !passwordHasher.Verify(request.Password, clinician.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var accessToken = tokenGenerator.GenerateAccessToken(clinician);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            ClinicianId = clinician.Id,
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
