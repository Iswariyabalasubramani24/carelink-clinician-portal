using CareLink.Application.Patients;
using CareLink.Domain.Entities;

namespace CareLink.UnitTests.Patients;

// Exercises the actual filtering logic (not a mock) via plain LINQ-to-Objects,
// since PatientSearchFilters.Apply operates on IQueryable<Patient> and the
// same predicates are used verbatim by PatientRepository.SearchAsync against
// EF Core. Note: string.Contains is case-sensitive under LINQ-to-Objects but
// case-insensitive when translated to SQL against the app's default
// (case-insensitive) collation - these tests deliberately match on exact
// substrings so the asserted behavior holds under both providers.
public class PatientSearchFiltersTests
{
    private static Patient MakePatient(
        int id,
        string firstName,
        string lastName,
        string mrn,
        DeviceType deviceType,
        DateTime implantDate,
        bool isActive = true) => new()
    {
        Id = id,
        TenantId = 1,
        MedicalRecordNumber = mrn,
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = new DateTime(1970, 1, 1),
        DeviceType = deviceType,
        DeviceSerialNumber = $"SN-{id}",
        ImplantDate = implantDate,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    private static List<Patient> SamplePatients() =>
    [
        MakePatient(1, "Rajesh", "Kumar", "APL-1001", DeviceType.ICD, new DateTime(2022, 3, 15)),
        MakePatient(2, "Priya", "Sharma", "APL-1002", DeviceType.Pacemaker, new DateTime(2023, 6, 1)),
        MakePatient(3, "Anil", "Verma", "APL-1003", DeviceType.ICD, new DateTime(2021, 1, 10), isActive: false),
        MakePatient(4, "Sunita", "Iyer", "APL-1004", DeviceType.CRT_D, new DateTime(2024, 8, 20))
    ];

    private static List<Patient> Filter(PatientSearchFilters filters) =>
        PatientSearchFilters.Apply(SamplePatients().AsQueryable(), filters).ToList();

    [Fact]
    public void Apply_NoFilters_ReturnsAllPatientsUnchanged()
    {
        var result = Filter(PatientSearchFilters.None);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Apply_DeviceTypeFilter_ReturnsOnlyMatchingDeviceType()
    {
        var result = Filter(new PatientSearchFilters(DeviceType.ICD, null, null, null, null));

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(DeviceType.ICD, p.DeviceType));
    }

    [Fact]
    public void Apply_ImplantDateRange_ReturnsOnlyPatientsImplantedWithinRange()
    {
        var filters = new PatientSearchFilters(null, new DateTime(2022, 1, 1), new DateTime(2023, 12, 31), null, null);

        var result = Filter(filters);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == 1);
        Assert.Contains(result, p => p.Id == 2);
    }

    [Fact]
    public void Apply_ImplantDateFromOnly_IsInclusiveOfTheBoundaryDate()
    {
        var filters = new PatientSearchFilters(null, new DateTime(2022, 3, 15), null, null, null);

        var result = Filter(filters);

        Assert.Contains(result, p => p.Id == 1);
    }

    [Fact]
    public void Apply_StatusFilter_ReturnsOnlyActiveOrInactiveAsRequested()
    {
        var activeOnly = Filter(new PatientSearchFilters(null, null, null, true, null));
        var inactiveOnly = Filter(new PatientSearchFilters(null, null, null, false, null));

        Assert.Equal(3, activeOnly.Count);
        Assert.DoesNotContain(activeOnly, p => p.Id == 3);

        Assert.Single(inactiveOnly);
        Assert.Equal(3, inactiveOnly[0].Id);
    }

    [Fact]
    public void Apply_KeywordMatchesFirstName_ReturnsMatchingPatient()
    {
        var result = Filter(new PatientSearchFilters(null, null, null, null, "Sunita"));

        Assert.Single(result);
        Assert.Equal(4, result[0].Id);
    }

    [Fact]
    public void Apply_KeywordMatchesLastName_ReturnsMatchingPatient()
    {
        var result = Filter(new PatientSearchFilters(null, null, null, null, "Verma"));

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
    }

    [Fact]
    public void Apply_KeywordMatchesMrn_ReturnsMatchingPatient()
    {
        var result = Filter(new PatientSearchFilters(null, null, null, null, "1002"));

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void Apply_KeywordWithNoMatches_ReturnsEmpty()
    {
        var result = Filter(new PatientSearchFilters(null, null, null, null, "NoSuchPatient"));

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_WhitespaceOnlyKeyword_IsTreatedAsNoKeywordFilter()
    {
        var result = Filter(new PatientSearchFilters(null, null, null, null, "   "));

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Apply_CombinedFilters_AreAndedTogether()
    {
        // ICD device type AND active status: patient 3 is ICD but inactive,
        // so combining both filters must exclude it even though each filter
        // alone would include a different subset.
        var filters = new PatientSearchFilters(DeviceType.ICD, null, null, true, null);

        var result = Filter(filters);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Apply_AllFiltersCombined_OnlyReturnsPatientMatchingEveryCriterion()
    {
        var filters = new PatientSearchFilters(
            DeviceType.ICD,
            new DateTime(2022, 1, 1),
            new DateTime(2022, 12, 31),
            true,
            "Kumar");

        var result = Filter(filters);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
