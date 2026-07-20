namespace CareLink.Application.Common.Exceptions;

public class PatientNotFoundException : Exception
{
    public PatientNotFoundException() : base("Patient not found.")
    {
    }
}
