namespace CareLink.Application.Common.Exceptions;

public class EmailAlreadyInUseException : Exception
{
    public EmailAlreadyInUseException() : base("A user with this email already exists.")
    {
    }
}
