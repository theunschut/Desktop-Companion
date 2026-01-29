using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Interfaces.IMonitors;
using MochiCompanion.Application.Interfaces.IServices;
using MochiCompanion.Domain.Entities;

namespace MochiCompanion.Application.Services;

/// <summary>
/// Service for coordinating multiple system monitors.
/// </summary>
public class MonitoringService : IMonitoringService
{
    private readonly IMoodService _moodService;
    private readonly ILogger<MonitoringService> _logger;
    private readonly List<ISystemMonitor> _monitors = new();
    private readonly Dictionary<ISystemMonitor, CancellationTokenSource> _monitorTokens = new();
    private readonly Dictionary<ISystemMonitor, Task> _monitorTasks = new();

    public IReadOnlyList<ISystemMonitor> Monitors => _monitors.AsReadOnly();

    public MonitoringService(IMoodService moodService, ILogger<MonitoringService> logger)
    {
        _moodService = moodService;
        _logger = logger;
    }

    public void RegisterMonitor(ISystemMonitor monitor)
    {
        if (!_monitors.Contains(monitor))
        {
            _monitors.Add(monitor);
            _logger.LogInformation("Registered monitor: {MonitorName}", monitor.Name);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var monitor in _monitors)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _monitorTokens[monitor] = cts;

            var task = Task.Run(() => RunMonitorAsync(monitor, cts.Token), cts.Token);
            _monitorTasks[monitor] = task;

            _logger.LogInformation("Started monitor: {MonitorName}", monitor.Name);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        // Signal all monitors to stop
        foreach (var cts in _monitorTokens.Values)
        {
            cts.Cancel();
        }

        // Wait for all tasks to complete (with timeout)
        var allTasks = _monitorTasks.Values.ToArray();
        try
        {
            await Task.WhenAll(allTasks).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Monitor shutdown timed out");
        }

        _monitorTokens.Clear();
        _monitorTasks.Clear();

        _logger.LogInformation("All monitors stopped");
    }

    private async Task RunMonitorAsync(ISystemMonitor monitor, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Monitor loop started: {MonitorName}", monitor.Name);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await monitor.CheckAsync(cancellationToken);

                if (result.HasSuggestion)
                {
                    await _moodService.ApplyMoodAsync(result.SuggestedMood!, cancellationToken);
                }

                await Task.Delay(monitor.CheckInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitor {MonitorName}", monitor.Name);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        _logger.LogInformation("Monitor loop stopped: {MonitorName}", monitor.Name);
    }
}
