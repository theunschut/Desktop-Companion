namespace MochiCompanion.Application.Exceptions;

/// <summary>
/// Exception thrown when mood management fails.
/// </summary>
public class MoodException : Exception
{
    public MoodException(string message) : base(message)
    {
    }

    public MoodException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
