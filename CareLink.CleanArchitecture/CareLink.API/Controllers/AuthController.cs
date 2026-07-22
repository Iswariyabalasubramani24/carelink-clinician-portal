using Asp.Versioning;
using CareLink.API.Contracts;
using CareLink.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareLink.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(IMediator mediator, IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("switch-tenant")]
    [Authorize]
    public async Task<ActionResult<SwitchTenantResult>> SwitchTenant(SwitchTenantRequest request)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? string.Empty;

        var result = await mediator.Send(new SwitchTenantCommand
        {
            ClinicianId = GetClinicianId(),
            RefreshToken = refreshToken,
            TenantId = request.TenantId
        });

        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
    {
        var result = await mediator.Send(command);

        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(LoginResponse.FromAuthResult(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var result = await mediator.Send(new RefreshTokenCommand { RefreshToken = refreshToken ?? string.Empty });

        // Sliding session: each successful refresh rotates the token, so
        // re-issue the cookie with the new value and extended expiry.
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(LoginResponse.FromRefreshResult(result));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await mediator.Send(new LogoutCommand { RefreshToken = refreshToken });
        }

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/v1/auth" });

        return NoContent();
    }

    private int GetClinicianId()
    {
        var claim = User.FindFirst("clinicianId")?.Value;
        return int.Parse(claim!);
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = expiresAt
        });
    }
}
