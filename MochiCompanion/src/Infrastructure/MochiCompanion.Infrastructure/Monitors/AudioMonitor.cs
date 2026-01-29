using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using MochiCompanion.Application.Interfaces.IMonitors;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.Infrastructure.Monitors;

/// <summary>
/// Monitors audio playback and suggests happy mood when music is playing.
/// Priority: 8 (Event layer)
/// </summary>
public class AudioMonitor : ISystemMonitor, IDisposable
{
    private readonly ILogger<AudioMonitor> _logger;
    private readonly MMDeviceEnumerator _deviceEnum;
    private MMDevice? _defaultDevice;
    private bool _wasPlayingMusic = false;

    public string Name => "AudioMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromSeconds(2);

    public AudioMonitor(ILogger<AudioMonitor> logger)
    {
        _logger = logger;
        _deviceEnum = new MMDeviceEnumerator();

        try
        {
            _defaultDevice = _deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get default audio device");
        }
    }

    public Task<MonitorResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isPlaying = IsMusicPlaying();

            if (isPlaying && !_wasPlayingMusic)
            {
                // Music just started
                _wasPlayingMusic = true;
                _logger.LogInformation("Music started playing");

                return Task.FromResult(MonitorResult.Suggest(
                    Name,
                    MoodType.Happy,
                    Priority.Event(8),
                    animation: AnimationType.Laugh,
                    duration: Duration.FromSeconds(5)
                ));
            }
            else if (!isPlaying && _wasPlayingMusic)
            {
                // Music stopped
                _wasPlayingMusic = false;
                _logger.LogInformation("Music stopped");
            }

            return Task.FromResult(MonitorResult.NoChange(Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking audio state");
            return Task.FromResult(MonitorResult.NoChange(Name));
        }
    }

    private bool IsMusicPlaying()
    {
        if (_defaultDevice == null)
            return false;

        try
        {
            var sessionManager = _defaultDevice.AudioSessionManager;
            var sessions = sessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];

                // Check if session has audio activity (volume > 0)
                var volume = session.SimpleAudioVolume?.Volume ?? 0;
                if (volume > 0)
                {
                    var processName = session.GetSessionIdentifier;
                    // Filter out system sounds
                    if (!string.IsNullOrEmpty(processName) &&
                        !processName.Contains("SystemSounds", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _defaultDevice?.Dispose();
        _deviceEnum?.Dispose();
    }
}
