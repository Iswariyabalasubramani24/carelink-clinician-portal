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
        RefreshTokenExpiryDays = 7
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

        var result = generator.GenerateAccessToken(clinician);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == "clinicianId").Value);
        Assert.Equal("7", jwt.Claims.First(c => c.Type == "tenantId").Value);
        Assert.Equal("doctor@apollo.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Admin", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("CareLinkAPI", jwt.Issuer);
        Assert.Contains("CareLinkClient", jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresApproximatelyFifteenMinutesFromNow()
    {
        var generator = MakeGenerator();
        var clinician = MakeClinician();

        var before = DateTime.UtcNow;
        var result = generator.GenerateAccessToken(clinician);

        var expectedExpiry = before.AddMinutes(15);
        Assert.True(Math.Abs((result.ExpiresAt - expectedExpiry).TotalSeconds) < 5);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokensWithSevenDayExpiry()
    {
        var generator = MakeGenerator();

        var first = generator.GenerateRefreshToken();
        var second = generator.GenerateRefreshToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.True(Math.Abs((first.ExpiresAt - DateTime.UtcNow.AddDays(7)).TotalSeconds) < 5);
    }
}
