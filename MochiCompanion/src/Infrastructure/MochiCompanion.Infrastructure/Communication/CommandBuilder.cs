using MochiCompanion.Application.Interfaces.ICommunication;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;

namespace MochiCompanion.Infrastructure.Communication;

/// <summary>
/// Builds command strings for the Mochi device following the serial protocol.
/// </summary>
public class CommandBuilder : ICommandBuilder
{
    public string BuildMoodCommand(MoodState moodState)
    {
        var moodName = $"MOOD_{moodState.Mood.ToString().ToUpper()}";
        var cmd = $"MOOD:{moodName}:{(int)moodState.Priority}";

        if (moodState.Duration != null && moodState.Duration.TotalSeconds > 0)
        {
            cmd += $":{moodState.Duration.TotalSeconds}";
        }

        return cmd;
    }

    public string BuildPositionCommand(PositionType position, int priority)
    {
        var posName = GetPositionName(position);
        return $"POS:{posName}:{priority}";
    }

    public string BuildAnimationCommand(AnimationType animation)
    {
        var animName = animation.ToString().ToUpper();
        return $"ANIM:{animName}";
    }

    public string BuildIdleCommand(bool enabled)
    {
        return $"IDLE:{(enabled ? "ON" : "OFF")}";
    }

    public string BuildBlinkCommand(bool enabled)
    {
        return $"BLINK:{(enabled ? "ON" : "OFF")}";
    }

    public string BuildResetCommand()
    {
        return "RESET";
    }

    private static string GetPositionName(PositionType position)
    {
        return position switch
        {
            PositionType.Center => "0",
            PositionType.North => "N",
            PositionType.NorthEast => "NE",
            PositionType.East => "E",
            PositionType.SouthEast => "SE",
            PositionType.South => "S",
            PositionType.SouthWest => "SW",
            PositionType.West => "W",
            PositionType.NorthWest => "NW",
            _ => "0"
        };
    }
}
