using CareLink.Domain.Entities;

namespace CareLink.Application.Patients;

// All filters are optional and combine with AND logic. Keyword is a simple
// Contains match (translates to SQL LIKE against a case-insensitive
// collation) across name/MRN - not a fuzzy/ranked search, by design: dozens
// of patients per tenant doesn't justify anything beyond SQL-based filtering.
public record PatientSearchFilters(
    DeviceType? DeviceType,
    DateTime? ImplantDateFrom,
    DateTime? ImplantDateTo,
    bool? IsActive,
    string? Keyword)
{
    public static readonly PatientSearchFilters None = new(null, null, null, null, null);

    // Operates on IQueryable<Patient> rather than a live DbContext so the
    // exact same filtering logic is exercised (not mocked) by both the real
    // EF Core provider and plain LINQ-to-Objects in unit tests.
    public static IQueryable<Patient> Apply(IQueryable<Patient> query, PatientSearchFilters filters)
    {
        if (filters.DeviceType is not null)
        {
            query = query.Where(p => p.DeviceType == filters.DeviceType);
        }

        if (filters.ImplantDateFrom is not null)
        {
            query = query.Where(p => p.ImplantDate >= filters.ImplantDateFrom.Value);
        }

        if (filters.ImplantDateTo is not null)
        {
            query = query.Where(p => p.ImplantDate <= filters.ImplantDateTo.Value);
        }

        if (filters.IsActive is not null)
        {
            query = query.Where(p => p.IsActive == filters.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Keyword))
        {
            var keyword = filters.Keyword.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(keyword) ||
                p.LastName.Contains(keyword) ||
                p.MedicalRecordNumber.Contains(keyword));
        }

        return query;
    }
}
