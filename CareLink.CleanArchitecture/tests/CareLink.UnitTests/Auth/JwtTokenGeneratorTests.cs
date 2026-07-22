using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareLink.Domain.Entities;
using CareLink.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace CareLink.UnitTests.Auth;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator MakeGenerator() => new(Options.Create(new JwtSettings
    {
        Secret = "unit-test-signing-secret-must-be-at-least-32-bytes-long-for-hmac-sha256",
        Issuer = "CareLinkAPI",
        Audience = "CareLinkClient",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenIdleTimeoutMinutes = 30
    }));

    private static Clinician MakeClinician() => new()
    {
        Id = 42,
        TenantId = 7,
        Email = "doctor@apollo.com",
        PasswordHash = "irrelevant-for-this-test",
        FirstName = "Anita",
        LastName = "Rao",
        Role = ClinicianRole.Admin,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void GenerateAccessToken_IncludesCorrectClaims()
    {
        var generator = MakeGenerator();
        var clinician = MakeClinician();

        var result = generator.GenerateAccessToken(clinician, clinician.TenantId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == "clinicianId").Value);
        Assert.Equal("7", jwt.Claims.First(c => c.Type == "tenantId").Value);
        Assert.Equal("doctor@apollo.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Admin", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("CareLinkAPI", jwt.Issuer);
        Assert.Contains("CareLinkClient", jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_UsesThePassedTenantIdNotTheCliniciansOwn()
    {
        // Guards the multi-hospital switching feature: the token must reflect
        // whichever tenant is currently active for the session, which can
        // differ from the clinician's own default TenantId after a switch.
        var generator = MakeGenerator();
        var clinician = MakeClinician();

        var result = generator.GenerateAccessToken(clinician, 99);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("99", jwt.Claims.First(c => c.Type == "tenantId").Value);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresApproximatelyFifteenMinutesFromNow()
    {
        var generator = MakeGenerator();
        var clinician = MakeClinician();

        var before = DateTime.UtcNow;
        var result = generator.GenerateAccessToken(clinician, clinician.TenantId);

        var expectedExpiry = before.AddMinutes(15);
        Assert.True(Math.Abs((result.ExpiresAt - expectedExpiry).TotalSeconds) < 5);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokensWithIdleWindowExpiry()
    {
        // The refresh token is a sliding idle window (30 min here), not a
        // long-lived "remember me" - clinical-app session hygiene.
        var generator = MakeGenerator();

        var first = generator.GenerateRefreshToken();
        var second = generator.GenerateRefreshToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.True(Math.Abs((first.ExpiresAt - DateTime.UtcNow.AddMinutes(30)).TotalSeconds) < 5);
    }
}
