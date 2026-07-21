namespace CareLink.Domain.Entities;

public enum ClinicianRole
{
    Clinician,
    Admin,

    // Platform operator. Not tied to a clinical hospital - a SuperAdmin belongs
    // to the hidden system tenant and provisions new hospitals (tenants) plus
    // each hospital's first Admin. Never used for patient-facing operations.
    SuperAdmin
}
