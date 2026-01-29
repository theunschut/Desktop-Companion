using MochiCompanion.Domain.Entities;

namespace MochiCompanion.Application.Interfaces.IServices;

/// <summary>
/// Service for managing Mochi's mood state with priority handling.
/// </summary>
public interface IMoodService
{
    /// <summary>
    /// Applies a mood suggestion if its priority is high enough.
    /// </summary>
    Task ApplyMoodAsync(MoodState moodState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active mood state.
    /// </summary>
    MoodState? CurrentMood { get; }

    /// <summary>
    /// Resets Mochi to the baseline mood (autonomous mode).
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for expired moods and reverts to baseline if needed.
    /// </summary>
    Task CheckExpiryAsync(CancellationToken cancellationToken = default);
}
