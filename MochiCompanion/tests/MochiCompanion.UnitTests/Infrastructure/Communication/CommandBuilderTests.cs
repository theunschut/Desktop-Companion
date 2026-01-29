using FluentAssertions;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;
using MochiCompanion.Infrastructure.Communication;

namespace MochiCompanion.UnitTests.Infrastructure.Communication;

public class CommandBuilderTests
{
    private readonly CommandBuilder _sut;

    public CommandBuilderTests()
    {
        _sut = new CommandBuilder();
    }

    [Fact]
    public void BuildMoodCommand_WithBasicMood_ShouldReturnCorrectFormat()
    {
        // Arrange
        var moodState = new MoodState(MoodType.Happy, new Priority(5));

        // Act
        var result = _sut.BuildMoodCommand(moodState);

        // Assert
        result.Should().Be("MOOD:MOOD_HAPPY:5");
    }

    [Fact]
    public void BuildMoodCommand_WithDuration_ShouldIncludeDuration()
    {
        // Arrange
        var moodState = new MoodState(
            MoodType.Tired,
            new Priority(3),
            duration: Duration.FromSeconds(10));

        // Act
        var result = _sut.BuildMoodCommand(moodState);

        // Assert
        result.Should().Be("MOOD:MOOD_TIRED:3:10");
    }

    [Theory]
    [InlineData(MoodType.Default, "MOOD:MOOD_DEFAULT:0")]
    [InlineData(MoodType.Happy, "MOOD:MOOD_HAPPY:0")]
    [InlineData(MoodType.Tired, "MOOD:MOOD_TIRED:0")]
    [InlineData(MoodType.Angry, "MOOD:MOOD_ANGRY:0")]
    public void BuildMoodCommand_WithDifferentMoods_ShouldReturnCorrectMoodName(
        MoodType mood, string expected)
    {
        // Arrange
        var moodState = new MoodState(mood, Priority.Baseline);

        // Act
        var result = _sut.BuildMoodCommand(moodState);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(PositionType.Center, 5, "POS:0:5")]
    [InlineData(PositionType.North, 3, "POS:N:3")]
    [InlineData(PositionType.NorthEast, 7, "POS:NE:7")]
    [InlineData(PositionType.East, 5, "POS:E:5")]
    [InlineData(PositionType.SouthEast, 5, "POS:SE:5")]
    [InlineData(PositionType.South, 5, "POS:S:5")]
    [InlineData(PositionType.SouthWest, 5, "POS:SW:5")]
    [InlineData(PositionType.West, 5, "POS:W:5")]
    [InlineData(PositionType.NorthWest, 5, "POS:NW:5")]
    public void BuildPositionCommand_WithDifferentPositions_ShouldReturnCorrectFormat(
        PositionType position, int priority, string expected)
    {
        // Act
        var result = _sut.BuildPositionCommand(position, priority);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(AnimationType.Blink, "ANIM:BLINK")]
    [InlineData(AnimationType.Confused, "ANIM:CONFUSED")]
    [InlineData(AnimationType.Laugh, "ANIM:LAUGH")]
    public void BuildAnimationCommand_WithDifferentAnimations_ShouldReturnCorrectFormat(
        AnimationType animation, string expected)
    {
        // Act
        var result = _sut.BuildAnimationCommand(animation);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "IDLE:ON")]
    [InlineData(false, "IDLE:OFF")]
    public void BuildIdleCommand_WithEnabledFlag_ShouldReturnCorrectFormat(
        bool enabled, string expected)
    {
        // Act
        var result = _sut.BuildIdleCommand(enabled);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "BLINK:ON")]
    [InlineData(false, "BLINK:OFF")]
    public void BuildBlinkCommand_WithEnabledFlag_ShouldReturnCorrectFormat(
        bool enabled, string expected)
    {
        // Act
        var result = _sut.BuildBlinkCommand(enabled);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void BuildResetCommand_ShouldReturnRESET()
    {
        // Act
        var result = _sut.BuildResetCommand();

        // Assert
        result.Should().Be("RESET");
    }
}
