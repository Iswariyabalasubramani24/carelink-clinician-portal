namespace CareLink.Application.Common.Exceptions;

public class AlertNotFoundException : Exception
{
    public AlertNotFoundException() : base("Alert not found.")
    {
    }
}
