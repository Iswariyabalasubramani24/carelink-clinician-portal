namespace CareLink.Application.Common.Exceptions;

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Invalid or expired refresh token.")
    {
    }
}
