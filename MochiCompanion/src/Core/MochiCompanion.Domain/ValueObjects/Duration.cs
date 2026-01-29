namespace MochiCompanion.Domain.ValueObjects;

/// <summary>
/// Represents a duration for a timed mood or animation.
/// </summary>
public record Duration
{
    public TimeSpan Value { get; init; }

    public Duration(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Duration cannot be negative");
        }

        Value = value;
    }

    public int TotalSeconds => (int)Value.TotalSeconds;
    public bool IsExpired(DateTime startTime) => DateTime.UtcNow >= startTime + Value;

    public static Duration FromSeconds(int seconds) => new(TimeSpan.FromSeconds(seconds));
    public static Duration FromMinutes(int minutes) => new(TimeSpan.FromMinutes(minutes));
    public static Duration Indefinite => new(TimeSpan.MaxValue);

    public static implicit operator TimeSpan(Duration duration) => duration.Value;
    public static implicit operator Duration(TimeSpan value) => new(value);
}
