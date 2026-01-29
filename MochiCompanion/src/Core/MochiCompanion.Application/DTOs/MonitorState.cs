namespace MochiCompanion.Application.DTOs;

/// <summary>
/// Data transfer object representing a monitor's current state.
/// </summary>
public record MonitorState
{
    public required string MonitorName { get; init; }
    public required bool IsRunning { get; init; }
    public DateTime? LastCheckTime { get; init; }
    public string? LastResult { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();

    public static MonitorState Create(
        string monitorName,
        bool isRunning,
        DateTime? lastCheckTime = null,
        string? lastResult = null)
    {
        return new MonitorState
        {
            MonitorName = monitorName,
            IsRunning = isRunning,
            LastCheckTime = lastCheckTime,
            LastResult = lastResult
        };
    }
}
