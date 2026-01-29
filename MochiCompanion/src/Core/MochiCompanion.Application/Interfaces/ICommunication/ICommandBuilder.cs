using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;

namespace MochiCompanion.Application.Interfaces.ICommunication;

/// <summary>
/// Builds command strings for the Mochi device.
/// </summary>
public interface ICommandBuilder
{
    /// <summary>
    /// Builds a MOOD command from a MoodState.
    /// </summary>
    string BuildMoodCommand(MoodState moodState);

    /// <summary>
    /// Builds a POS (position) command.
    /// </summary>
    string BuildPositionCommand(PositionType position, int priority);

    /// <summary>
    /// Builds an ANIM (animation) command.
    /// </summary>
    string BuildAnimationCommand(AnimationType animation);

    /// <summary>
    /// Builds an IDLE command.
    /// </summary>
    string BuildIdleCommand(bool enabled);

    /// <summary>
    /// Builds a BLINK command.
    /// </summary>
    string BuildBlinkCommand(bool enabled);

    /// <summary>
    /// Builds a RESET command.
    /// </summary>
    string BuildResetCommand();
}
