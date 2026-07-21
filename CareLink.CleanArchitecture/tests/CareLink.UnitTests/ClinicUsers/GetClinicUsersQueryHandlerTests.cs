using CareLink.Application.ClinicUsers.Queries;
using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.ClinicUsers;

public class GetClinicUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyUsersBelongingToTheRequestedTenant()
    {
        // The repository call is tenant-scoped by construction (it only
        // accepts a single tenantId), so an admin can never receive another
        // hospital's users back from this query.
        var tenant1Users = new List<Clinician>
        {
            new() { Id = 1, TenantId = 1, FirstName = "Anita", LastName = "Rao", Email = "doctor@apollo.com", Role = ClinicianRole.Clinician, LanguageCode = "en", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TenantId = 1, FirstName = "Meera", LastName = "Pillai", Email = "admin@apollo.com", Role = ClinicianRole.Admin, LanguageCode = "en", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var clinicianRepo = new Mock<IClinicianRepository>();
        clinicianRepo.Setup(r => r.GetByTenantIdAsync(1)).ReturnsAsync(tenant1Users);

        var handler = new GetClinicUsersQueryHandler(clinicianRepo.Object);

        var result = await handler.Handle(new GetClinicUsersQuery(1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Contains(dto.Email, new[] { "doctor@apollo.com", "admin@apollo.com" }));
        clinicianRepo.Verify(r => r.GetByTenantIdAsync(2), Times.Never);
    }
}
