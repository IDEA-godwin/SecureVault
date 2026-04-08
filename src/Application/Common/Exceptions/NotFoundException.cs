namespace SecureVault.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found in the database or system.
/// Maps to HTTP 404 Not Found response.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException() : base() { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}
