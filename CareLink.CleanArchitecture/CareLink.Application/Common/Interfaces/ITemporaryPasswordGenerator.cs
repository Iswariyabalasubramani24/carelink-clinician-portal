namespace CareLink.Application.Common.Interfaces;

public interface ITemporaryPasswordGenerator
{
    // Returns a plaintext one-time password - the caller hashes it for storage
    // and returns the plaintext to the admin exactly once (never persisted or logged).
    string Generate();
}
