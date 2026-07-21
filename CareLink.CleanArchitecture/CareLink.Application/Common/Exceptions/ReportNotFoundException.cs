namespace CareLink.Application.Common.Exceptions;

public class ReportNotFoundException : Exception
{
    public ReportNotFoundException() : base("Report not found.")
    {
    }
}
