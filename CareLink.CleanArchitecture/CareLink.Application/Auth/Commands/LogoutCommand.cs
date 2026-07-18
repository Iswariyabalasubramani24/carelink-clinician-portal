using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.Auth.Commands;

public class LogoutCommand : IRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            await refreshTokenRepository.RevokeAsync(storedToken);
        }
    }
}
