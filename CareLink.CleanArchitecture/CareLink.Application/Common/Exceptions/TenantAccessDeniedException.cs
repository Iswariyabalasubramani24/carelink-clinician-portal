namespace CareLink.Application.Common.Exceptions;

public class TenantAccessDeniedException : Exception
{
    public TenantAccessDeniedException() : base("You do not have access to this hospital.")
    {
    }
}
