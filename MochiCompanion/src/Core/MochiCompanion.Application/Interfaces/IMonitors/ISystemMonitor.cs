using MochiCompanion.Domain.Entities;

namespace MochiCompanion.Application.Interfaces.IMonitors;

/// <summary>
/// Base interface for system monitors that suggest mood changes.
/// </summary>
public interface ISystemMonitor
{
    /// <summary>
    /// Checks the current state and returns a mood suggestion if applicable.
    /// </summary>
    /// <returns>Monitor result with optional mood suggestion</returns>
    Task<MonitorResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// How frequently this monitor should check (in milliseconds).
    /// </summary>
    TimeSpan CheckInterval { get; }

    /// <summary>
    /// Name of this monitor for logging and identification.
    /// </summary>
    string Name { get; }
}
