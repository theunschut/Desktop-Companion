using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Interfaces.IMonitors;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Infrastructure.Monitors;

/// <summary>
/// Monitors time of day and suggests moods based on schedule.
/// Priority: 1-3 (Time-based layer)
/// </summary>
public class TimeMonitor : ISystemMonitor
{
    private readonly ILogger<TimeMonitor> _logger;
    private int _lastHour = -1;

    public string Name => "TimeMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromMinutes(1);

    public TimeMonitor(ILogger<TimeMonitor> logger)
    {
        _logger = logger;
    }

    public Task<MonitorResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var hour = now.Hour;

        // Only suggest mood change when hour changes
        if (hour == _lastHour)
        {
            return Task.FromResult(MonitorResult.NoChange(Name));
        }

        _lastHour = hour;

        var result = hour switch
        {
            >= 6 and < 9 => MonitorResult.Suggest(
                Name,
                MoodType.Tired,
                Priority.TimeBased(2),
                position: PositionType.South), // Sleepy, looking down

            >= 9 and < 18 => MonitorResult.Suggest(
                Name,
                MoodType.Default,
                Priority.TimeBased(1)),

            >= 18 and < 22 => MonitorResult.Suggest(
                Name,
                MoodType.Happy,
                Priority.TimeBased(1)),

            _ => MonitorResult.Suggest(
                Name,
                MoodType.Tired,
                Priority.TimeBased(3),
                position: PositionType.South) // Very sleepy at night
        };

        _logger.LogDebug("Time check: {Hour}:00 → {Mood}",
            hour, result.SuggestedMood?.Mood);

        return Task.FromResult(result);
    }
}
