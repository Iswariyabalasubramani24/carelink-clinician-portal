namespace CareLink.Domain.Entities;

// Junction entity granting a Clinician access to a Tenant (hospital). This is
// the source of truth for multi-hospital access control; Clinician.TenantId
// remains a separate "default tenant" fast-lookup used only at login.
public class ClinicianTenant
{
    public int Id { get; set; }

    public int ClinicianId { get; set; }

    public int TenantId { get; set; }

    public Clinician Clinician { get; set; } = null!;

    public Tenant Tenant { get; set; } = null!;
}
