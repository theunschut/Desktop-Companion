using MochiCompanion.Application.Interfaces.IMonitors;

namespace MochiCompanion.Application.Interfaces.IServices;

/// <summary>
/// Service for coordinating multiple system monitors.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Starts all registered monitors.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all monitors.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a monitor to be run.
    /// </summary>
    void RegisterMonitor(ISystemMonitor monitor);

    /// <summary>
    /// Gets all registered monitors.
    /// </summary>
    IReadOnlyList<ISystemMonitor> Monitors { get; }
}
