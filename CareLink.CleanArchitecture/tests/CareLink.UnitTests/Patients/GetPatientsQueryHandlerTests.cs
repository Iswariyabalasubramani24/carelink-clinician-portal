using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients.Queries;
using CareLink.Domain.Entities;
using Moq;

namespace CareLink.UnitTests.Patients;

public class GetPatientsQueryHandlerTests
{
    private static Patient MakePatient(int id, int tenantId, string firstName, string lastName) => new()
    {
        Id = id,
        TenantId = tenantId,
        MedicalRecordNumber = $"MRN-{id}",
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = DeviceType.Pacemaker,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = new DateTime(2020, 1, 1),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_TenantWithPatients_ReturnsOnlyThatTenantsPatients()
    {
        var tenant1Patients = new List<Patient>
        {
            MakePatient(1, tenantId: 1, "Rajesh", "Kumar"),
            MakePatient(2, tenantId: 1, "Priya", "Sharma")
        };

        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.GetByTenantIdAsync(1))
            .ReturnsAsync(tenant1Patients);

        var handler = new GetPatientsQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetPatientsQuery(1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(1, dto.TenantId));
        Assert.Contains(result, dto => dto.FirstName == "Rajesh");
        Assert.Contains(result, dto => dto.FirstName == "Priya");
        repositoryMock.Verify(r => r.GetByTenantIdAsync(1), Times.Once);
        repositoryMock.Verify(r => r.GetByTenantIdAsync(It.Is<int>(id => id != 1)), Times.Never);
    }

    [Fact]
    public async Task Handle_TenantWithNoPatients_ReturnsEmptyList()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.GetByTenantIdAsync(99))
            .ReturnsAsync(new List<Patient>());

        var handler = new GetPatientsQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetPatientsQuery(99), CancellationToken.None);

        Assert.Empty(result);
        repositoryMock.Verify(r => r.GetByTenantIdAsync(99), Times.Once);
    }
}
