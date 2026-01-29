using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Domain.Entities;

/// <summary>
/// Represents the current mood state of Mochi including priority and expiry.
/// </summary>
public class MoodState
{
    public MoodType Mood { get; private set; }
    public Priority Priority { get; private set; }
    public PositionType? Position { get; private set; }
    public AnimationType? Animation { get; private set; }
    public Duration? Duration { get; private set; }
    public DateTime Timestamp { get; private set; }

    public MoodState(
        MoodType mood,
        Priority priority,
        PositionType? position = null,
        AnimationType? animation = null,
        Duration? duration = null)
    {
        Mood = mood;
        Priority = priority;
        Position = position;
        Animation = animation;
        Duration = duration;
        Timestamp = DateTime.UtcNow;
    }

    public bool IsExpired => Duration != null && Duration.IsExpired(Timestamp);

    public bool CanOverride(MoodState? currentState)
    {
        if (currentState == null) return true;
        if (currentState.IsExpired) return true;
        return Priority.IsHigherThan(currentState.Priority);
    }

    public MoodState WithMood(MoodType mood) => new(
        mood,
        Priority,
        Position,
        Animation,
        Duration
    );

    public MoodState WithPriority(Priority priority) => new(
        Mood,
        priority,
        Position,
        Animation,
        Duration
    );

    public static MoodState CreateBaseline() => new(
        MoodType.Default,
        Priority.Baseline
    );
}
