namespace MochiCompanion.Application.Exceptions;

/// <summary>
/// Exception thrown when communication with Mochi device fails.
/// </summary>
public class ConnectionException : Exception
{
    public string? Address { get; }

    public ConnectionException(string message) : base(message)
    {
    }

    public ConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConnectionException(string message, string address)
        : base(message)
    {
        Address = address;
    }

    public ConnectionException(string message, string address, Exception innerException)
        : base(message, innerException)
    {
        Address = address;
    }
}
