using CareLink.Application.Common.Interfaces;
using CareLink.Application.Patients;
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
            .Setup(r => r.SearchAsync(1, It.IsAny<PatientSearchFilters>()))
            .ReturnsAsync(tenant1Patients);

        var handler = new GetPatientsQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetPatientsQuery(1), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(1, dto.TenantId));
        Assert.Contains(result, dto => dto.FirstName == "Rajesh");
        Assert.Contains(result, dto => dto.FirstName == "Priya");
        repositoryMock.Verify(r => r.SearchAsync(1, It.IsAny<PatientSearchFilters>()), Times.Once);
        repositoryMock.Verify(r => r.SearchAsync(It.Is<int>(id => id != 1), It.IsAny<PatientSearchFilters>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TenantWithNoPatients_ReturnsEmptyList()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.SearchAsync(99, It.IsAny<PatientSearchFilters>()))
            .ReturnsAsync(new List<Patient>());

        var handler = new GetPatientsQueryHandler(repositoryMock.Object);

        var result = await handler.Handle(new GetPatientsQuery(99), CancellationToken.None);

        Assert.Empty(result);
        repositoryMock.Verify(r => r.SearchAsync(99, It.IsAny<PatientSearchFilters>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoFiltersSet_PassesAllNullFiltersToRepository()
    {
        // "Empty filters return all patients" is enforced by the repository
        // (PatientSearchFiltersTests covers that directly) - this test just
        // confirms the handler doesn't invent any default filter values.
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.SearchAsync(1, It.IsAny<PatientSearchFilters>()))
            .ReturnsAsync(new List<Patient>());

        var handler = new GetPatientsQueryHandler(repositoryMock.Object);

        await handler.Handle(new GetPatientsQuery(1), CancellationToken.None);

        repositoryMock.Verify(r => r.SearchAsync(1, PatientSearchFilters.None), Times.Once);
    }

    [Fact]
    public async Task Handle_FiltersProvided_PassesThemThroughUnchangedToRepository()
    {
        var repositoryMock = new Mock<IPatientRepository>();
        repositoryMock
            .Setup(r => r.SearchAsync(1, It.IsAny<PatientSearchFilters>()))
            .ReturnsAsync(new List<Patient>());

        var handler = new GetPatientsQueryHandler(repositoryMock.Object);

        var from = new DateTime(2022, 1, 1);
        var to = new DateTime(2023, 1, 1);
        await handler.Handle(
            new GetPatientsQuery(1)
            {
                DeviceType = DeviceType.ICD,
                ImplantDateFrom = from,
                ImplantDateTo = to,
                IsActive = true,
                Keyword = "kumar"
            },
            CancellationToken.None);

        repositoryMock.Verify(r => r.SearchAsync(
            1,
            new PatientSearchFilters(DeviceType.ICD, from, to, true, "kumar")),
            Times.Once);
    }
}
