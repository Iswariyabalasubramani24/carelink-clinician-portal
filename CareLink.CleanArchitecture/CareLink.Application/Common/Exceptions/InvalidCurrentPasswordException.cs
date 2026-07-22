namespace CareLink.Application.Common.Exceptions;

// Distinct from InvalidCredentialsException (401) so a wrong "current
// password" during a password change surfaces as a form error (400), not as
// a session problem.
public class InvalidCurrentPasswordException : Exception
{
    public InvalidCurrentPasswordException() : base("The current password is incorrect.")
    {
    }
}
