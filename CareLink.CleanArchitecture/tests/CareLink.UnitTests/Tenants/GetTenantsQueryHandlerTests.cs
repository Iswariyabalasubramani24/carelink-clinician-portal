using CareLink.Application.Common.Interfaces;
using CareLink.Application.Tenants.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Tenants;

public class GetTenantsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllActiveTenantsMappedToDto()
    {
        var tenants = new List<Tenant>
        {
            new() { Id = 1, Name = "Apollo Hospital", Region = "India", LanguageCode = "en", IsActive = true },
            new() { Id = 2, Name = "Charite Hospital", Region = "Germany", LanguageCode = "de", IsActive = true }
        };

        var repository = new Mock<ITenantRepository>();
        repository.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(tenants);

        var handler = new GetTenantsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetTenantsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == 1 && t.Name == "Apollo Hospital" && t.Region == "India" && t.LanguageCode == "en");
        Assert.Contains(result, t => t.Id == 2 && t.Name == "Charite Hospital" && t.Region == "Germany" && t.LanguageCode == "de");
    }

    [Fact]
    public async Task Handle_NoActiveTenants_ReturnsEmptyList()
    {
        var repository = new Mock<ITenantRepository>();
        repository.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new List<Tenant>());

        var handler = new GetTenantsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetTenantsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
