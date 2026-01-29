using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Interfaces.IMonitors;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Infrastructure.Monitors;

/// <summary>
/// Monitors active application/window and suggests moods based on context.
/// Priority: 5-7 (System State/Event hybrid)
/// </summary>
public class ApplicationMonitor : ISystemMonitor
{
    private readonly ILogger<ApplicationMonitor> _logger;
    private string _lastActiveWindow = "";

    public string Name => "ApplicationMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromSeconds(3);

    public ApplicationMonitor(ILogger<ApplicationMonitor> logger)
    {
        _logger = logger;
    }

    public Task<MonitorResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeWindow = GetActiveWindowTitle();

            // Only react when window changes
            if (activeWindow == _lastActiveWindow)
            {
                return Task.FromResult(MonitorResult.NoChange(Name));
            }

            _lastActiveWindow = activeWindow;

            // Check for specific applications
            if (activeWindow.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase) ||
                activeWindow.Contains("VS Code", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Coding app detected: {App}", activeWindow);
                return Task.FromResult(MonitorResult.Suggest(
                    Name,
                    MoodType.Default,
                    Priority.SystemState(6),
                    position: PositionType.North // Looking up (focused)
                ));
            }
            else if (activeWindow.Contains("Spotify", StringComparison.OrdinalIgnoreCase) ||
                     activeWindow.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Media app detected: {App}", activeWindow);
                return Task.FromResult(MonitorResult.Suggest(
                    Name,
                    MoodType.Happy,
                    Priority.Event(7)
                ));
            }

            return Task.FromResult(MonitorResult.NoChange(Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active application");
            return Task.FromResult(MonitorResult.NoChange(Name));
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private string GetActiveWindowTitle()
    {
        const int nChars = 256;
        var buff = new StringBuilder(nChars);
        var handle = GetForegroundWindow();

        if (GetWindowText(handle, buff, nChars) > 0)
        {
            return buff.ToString();
        }

        return string.Empty;
    }
}
