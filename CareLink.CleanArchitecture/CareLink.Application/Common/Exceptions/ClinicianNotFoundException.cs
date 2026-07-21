namespace CareLink.Application.Common.Exceptions;

public class ClinicianNotFoundException : Exception
{
    public ClinicianNotFoundException() : base("Clinician not found.")
    {
    }
}
