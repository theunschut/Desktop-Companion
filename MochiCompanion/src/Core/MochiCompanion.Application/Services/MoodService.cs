using Microsoft.Extensions.Logging;
using MochiCompanion.Application.Exceptions;
using MochiCompanion.Application.Interfaces.ICommunication;
using MochiCompanion.Application.Interfaces.IServices;
using MochiCompanion.Domain.Entities;

namespace MochiCompanion.Application.Services;

/// <summary>
/// Service for managing Mochi's mood state with priority-based handling.
/// </summary>
public class MoodService : IMoodService
{
    private readonly IMochiConnection _connection;
    private readonly ICommandBuilder _commandBuilder;
    private readonly ILogger<MoodService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private MoodState? _currentMood;

    public MoodState? CurrentMood => _currentMood;

    public MoodService(
        IMochiConnection connection,
        ICommandBuilder commandBuilder,
        ILogger<MoodService> logger)
    {
        _connection = connection;
        _commandBuilder = commandBuilder;
        _logger = logger;
    }

    public async Task ApplyMoodAsync(MoodState moodState, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Check if new mood can override current mood
            if (!moodState.CanOverride(_currentMood))
            {
                _logger.LogDebug(
                    "Ignoring mood {Mood} (pri {Priority}) - current mood has higher priority {CurrentPriority}",
                    moodState.Mood, moodState.Priority, _currentMood?.Priority);
                return;
            }

            // Send commands to Mochi
            if (!_connection.IsConnected)
            {
                throw new ConnectionException("Not connected to Mochi device");
            }

            // Send mood command
            var moodCmd = _commandBuilder.BuildMoodCommand(moodState);
            await _connection.SendCommandAsync(moodCmd);

            // Send optional position command
            if (moodState.Position != null)
            {
                var posCmd = _commandBuilder.BuildPositionCommand(
                    moodState.Position.Value,
                    moodState.Priority);
                await _connection.SendCommandAsync(posCmd);
            }

            // Send optional animation command
            if (moodState.Animation != null)
            {
                var animCmd = _commandBuilder.BuildAnimationCommand(moodState.Animation.Value);
                await _connection.SendCommandAsync(animCmd);
            }

            _currentMood = moodState;

            _logger.LogInformation(
                "Applied mood: {Mood} (priority {Priority})",
                moodState.Mood, moodState.Priority);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task CheckExpiryAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_currentMood != null && _currentMood.IsExpired)
            {
                _logger.LogInformation("Mood {Mood} expired, resetting to baseline", _currentMood.Mood);
                await ResetInternalAsync(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await ResetInternalAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task ResetInternalAsync(CancellationToken cancellationToken)
    {
        if (!_connection.IsConnected)
        {
            throw new ConnectionException("Not connected to Mochi device");
        }

        var resetCmd = _commandBuilder.BuildResetCommand();
        await _connection.SendCommandAsync(resetCmd);

        _currentMood = MoodState.CreateBaseline();
        _logger.LogInformation("Reset to baseline mood");
    }
}
