using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.UnitTests.TestHelpers;

/// <summary>
/// Test builder for creating MoodState instances with fluent API.
/// </summary>
public class MoodStateBuilder
{
    private MoodType _mood = MoodType.Default;
    private Priority _priority = Priority.Baseline;
    private PositionType? _position;
    private AnimationType? _animation;
    private Duration? _duration;

    public MoodStateBuilder WithMood(MoodType mood)
    {
        _mood = mood;
        return this;
    }

    public MoodStateBuilder WithPriority(int priority)
    {
        _priority = new Priority(priority);
        return this;
    }

    public MoodStateBuilder WithPosition(PositionType position)
    {
        _position = position;
        return this;
    }

    public MoodStateBuilder WithAnimation(AnimationType animation)
    {
        _animation = animation;
        return this;
    }

    public MoodStateBuilder WithDuration(int seconds)
    {
        _duration = Duration.FromSeconds(seconds);
        return this;
    }

    public MoodStateBuilder AsHappy() => WithMood(MoodType.Happy);
    public MoodStateBuilder AsTired() => WithMood(MoodType.Tired);
    public MoodStateBuilder AsAngry() => WithMood(MoodType.Angry);

    public MoodStateBuilder AsTimeBased(int level = 2) => WithPriority(level);
    public MoodStateBuilder AsSystemState(int level = 5) => WithPriority(level);
    public MoodStateBuilder AsEvent(int level = 8) => WithPriority(level);

    public MoodState Build()
    {
        return new MoodState(_mood, _priority, _position, _animation, _duration);
    }

    public static implicit operator MoodState(MoodStateBuilder builder) => builder.Build();
}
