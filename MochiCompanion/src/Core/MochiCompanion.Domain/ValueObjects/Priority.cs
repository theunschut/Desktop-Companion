namespace MochiCompanion.Domain.ValueObjects;

/// <summary>
/// Represents a priority level for mood suggestions.
/// Priority 0: Baseline (autonomous behaviors)
/// Priority 1-3: Time-based moods
/// Priority 4-6: System state moods
/// Priority 7-10: Event-driven moods
/// </summary>
public record Priority
{
    public int Value { get; init; }

    public Priority(int value)
    {
        if (value < 0 || value > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Priority must be between 0 and 10");
        }

        Value = value;
    }

    public static Priority Baseline => new(0);
    public static Priority TimeBased(int level = 2) => new(Math.Clamp(level, 1, 3));
    public static Priority SystemState(int level = 5) => new(Math.Clamp(level, 4, 6));
    public static Priority Event(int level = 8) => new(Math.Clamp(level, 7, 10));

    public bool IsHigherThan(Priority other) => Value > other.Value;

    public static implicit operator int(Priority priority) => priority.Value;
    public static implicit operator Priority(int value) => new(value);
}
