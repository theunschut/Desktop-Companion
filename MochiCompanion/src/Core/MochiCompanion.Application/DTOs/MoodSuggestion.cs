using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Application.DTOs;

/// <summary>
/// Data transfer object for mood suggestions from monitors.
/// Simplified version of MoodState for external use.
/// </summary>
public record MoodSuggestion
{
    public required MoodType Mood { get; init; }
    public required int Priority { get; init; }
    public PositionType? Position { get; init; }
    public AnimationType? Animation { get; init; }
    public TimeSpan? Duration { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? Source { get; init; }

    public static MoodSuggestion Create(
        MoodType mood,
        int priority,
        PositionType? position = null,
        AnimationType? animation = null,
        TimeSpan? duration = null,
        string? source = null)
    {
        return new MoodSuggestion
        {
            Mood = mood,
            Priority = priority,
            Position = position,
            Animation = animation,
            Duration = duration,
            Source = source
        };
    }
}
