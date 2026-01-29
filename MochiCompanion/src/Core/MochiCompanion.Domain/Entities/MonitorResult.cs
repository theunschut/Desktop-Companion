using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Domain.Entities;

/// <summary>
/// Represents the result from a monitor check, containing mood suggestion if any.
/// </summary>
public class MonitorResult
{
    public string MonitorName { get; init; }
    public MoodState? SuggestedMood { get; init; }
    public DateTime CheckedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; }

    public MonitorResult(string monitorName, MoodState? suggestedMood = null)
    {
        MonitorName = monitorName;
        SuggestedMood = suggestedMood;
        CheckedAt = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    public bool HasSuggestion => SuggestedMood != null;

    public MonitorResult WithMetadata(string key, object value)
    {
        var result = new MonitorResult(MonitorName, SuggestedMood)
        {
            CheckedAt = CheckedAt
        };

        foreach (var kvp in Metadata)
        {
            result.Metadata[kvp.Key] = kvp.Value;
        }

        result.Metadata[key] = value;
        return result;
    }

    public static MonitorResult NoChange(string monitorName) => new(monitorName, null);

    public static MonitorResult Suggest(
        string monitorName,
        MoodType mood,
        Priority priority,
        PositionType? position = null,
        AnimationType? animation = null,
        Duration? duration = null)
    {
        var moodState = new MoodState(mood, priority, position, animation, duration);
        return new MonitorResult(monitorName, moodState);
    }
}
