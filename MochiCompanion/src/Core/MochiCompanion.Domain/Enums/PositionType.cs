namespace MochiCompanion.Domain.Enums;

/// <summary>
/// Represents the eye position/gaze direction of Mochi.
/// Maps to Arduino position constants.
/// </summary>
public enum PositionType
{
    Center = 0,
    North = 1,      // Up
    NorthEast = 2,
    East = 3,       // Right
    SouthEast = 4,
    South = 5,      // Down
    SouthWest = 6,
    West = 7,       // Left
    NorthWest = 8
}
