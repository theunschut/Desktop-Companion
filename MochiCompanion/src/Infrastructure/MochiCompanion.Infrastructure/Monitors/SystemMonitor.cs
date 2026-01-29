using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Interfaces.IMonitors;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Infrastructure.Monitors;

/// <summary>
/// Monitors system resources (CPU usage) and suggests moods based on load.
/// Priority: 4-6 (System State layer)
/// </summary>
public class SystemMonitor : ISystemMonitor, IDisposable
{
    private readonly ILogger<SystemMonitor> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private float _lastCpuUsage = 0;

    public string Name => "SystemMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromSeconds(5);

    public SystemMonitor(ILogger<SystemMonitor> logger)
    {
        _logger = logger;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        // First call always returns 0, so prime it
        _cpuCounter.NextValue();
    }

    public Task<MonitorResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cpuUsage = _cpuCounter.NextValue();

            // Only react to significant changes (more than 10% difference)
            if (Math.Abs(cpuUsage - _lastCpuUsage) < 10)
            {
                return Task.FromResult(MonitorResult.NoChange(Name));
            }

            _lastCpuUsage = cpuUsage;

            if (cpuUsage > 80)
            {
                _logger.LogInformation("High CPU usage: {Cpu}%", cpuUsage);
                return Task.FromResult(MonitorResult.Suggest(
                    Name,
                    MoodType.Tired,
                    Priority.SystemState(5),
                    position: PositionType.South // Looking down when tired
                ));
            }
            else if (cpuUsage < 20)
            {
                return Task.FromResult(MonitorResult.Suggest(
                    Name,
                    MoodType.Default,
                    Priority.SystemState(4)
                ));
            }

            return Task.FromResult(MonitorResult.NoChange(Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system state");
            return Task.FromResult(MonitorResult.NoChange(Name));
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
    }
}
