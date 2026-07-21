namespace CareLink.Application.Common.Exceptions;

public class AccountSuspendedException : Exception
{
    public AccountSuspendedException() : base("Your account has been suspended. Please contact your clinic administrator.")
    {
    }
}
