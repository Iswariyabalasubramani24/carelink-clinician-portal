using CareLink.Application.Common.Interfaces;
using CareLink.Application.Hospitals.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Hospitals;

public class GetHospitalsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsTenantsAndClinicianCountsIncludingInactiveHospitals()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllWithClinicianCountAsync()).ReturnsAsync(
        [
            (new Tenant { Id = 1, Name = "Apollo Hospital", Region = "India", LanguageCode = "en", IsActive = true }, 5),
            (new Tenant { Id = 2, Name = "Charite Hospital", Region = "Germany", LanguageCode = "de", IsActive = false }, 2)
        ]);

        var handler = new GetHospitalsQueryHandler(repo.Object);

        var result = await handler.Handle(new GetHospitalsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, h => h.Id == 1 && h.Name == "Apollo Hospital" && h.Region == "India" && h.IsActive && h.ClinicianCount == 5);
        // Inactive hospitals stay visible to the super-admin - this is the
        // management view, unlike the public login picker.
        Assert.Contains(result, h => h.Id == 2 && !h.IsActive && h.ClinicianCount == 2);
    }

    [Fact]
    public async Task Handle_NoHospitals_ReturnsEmptyList()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllWithClinicianCountAsync()).ReturnsAsync([]);

        var handler = new GetHospitalsQueryHandler(repo.Object);

        var result = await handler.Handle(new GetHospitalsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
