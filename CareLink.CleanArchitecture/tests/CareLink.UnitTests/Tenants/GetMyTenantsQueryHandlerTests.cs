using CareLink.Application.Common.Interfaces;
using CareLink.Application.Tenants.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Tenants;

public class GetMyTenantsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MultiTenantClinician_ReturnsAllAccessibleTenants()
    {
        var tenants = new List<Tenant>
        {
            new() { Id = 1, Name = "Apollo Hospital", Region = "India", LanguageCode = "en", IsActive = true },
            new() { Id = 2, Name = "Charite Hospital", Region = "Germany", LanguageCode = "de", IsActive = true }
        };

        var repository = new Mock<IClinicianTenantRepository>();
        repository.Setup(r => r.GetTenantsForClinicianAsync(1)).ReturnsAsync(tenants);

        var handler = new GetMyTenantsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMyTenantsQuery { ClinicianId = 1 }, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == 1 && t.Name == "Apollo Hospital");
        Assert.Contains(result, t => t.Id == 2 && t.Name == "Charite Hospital");
    }

    [Fact]
    public async Task Handle_SingleTenantClinician_ReturnsOnlyThatTenant()
    {
        var tenants = new List<Tenant>
        {
            new() { Id = 1, Name = "Apollo Hospital", Region = "India", LanguageCode = "en", IsActive = true }
        };

        var repository = new Mock<IClinicianTenantRepository>();
        repository.Setup(r => r.GetTenantsForClinicianAsync(2)).ReturnsAsync(tenants);

        var handler = new GetMyTenantsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMyTenantsQuery { ClinicianId = 2 }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Apollo Hospital", result[0].Name);
    }
}
