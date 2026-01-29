using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MochiCompanion.Application.Interfaces.ICommunication;
using MochiCompanion.Application.Services;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;

namespace MochiCompanion.UnitTests.Application.Services;

public class MoodServiceTests
{
    private readonly Mock<IMochiConnection> _mockConnection;
    private readonly Mock<ICommandBuilder> _mockCommandBuilder;
    private readonly Mock<ILogger<MoodService>> _mockLogger;
    private readonly MoodService _sut;

    public MoodServiceTests()
    {
        _mockConnection = new Mock<IMochiConnection>();
        _mockCommandBuilder = new Mock<ICommandBuilder>();
        _mockLogger = new Mock<ILogger<MoodService>>();

        _mockConnection.Setup(x => x.IsConnected).Returns(true);

        _sut = new MoodService(
            _mockConnection.Object,
            _mockCommandBuilder.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ApplyMoodAsync_WhenHigherPriority_ShouldApplyMood()
    {
        // Arrange
        var moodState = new MoodState(MoodType.Happy, new Priority(5));
        var expectedCommand = "MOOD:MOOD_HAPPY:5";

        _mockCommandBuilder
            .Setup(x => x.BuildMoodCommand(It.IsAny<MoodState>()))
            .Returns(expectedCommand);

        // Act
        await _sut.ApplyMoodAsync(moodState);

        // Assert
        _mockConnection.Verify(x => x.SendCommandAsync(expectedCommand), Times.Once);
        _sut.CurrentMood.Should().NotBeNull();
        _sut.CurrentMood!.Mood.Should().Be(MoodType.Happy);
        _sut.CurrentMood.Priority.Value.Should().Be(5);
    }

    [Fact]
    public async Task ApplyMoodAsync_WhenLowerPriority_ShouldIgnoreMood()
    {
        // Arrange
        var highPriorityMood = new MoodState(MoodType.Happy, new Priority(8));
        var lowPriorityMood = new MoodState(MoodType.Tired, new Priority(3));

        _mockCommandBuilder
            .Setup(x => x.BuildMoodCommand(It.IsAny<MoodState>()))
            .Returns("MOOD:MOOD_HAPPY:8");

        // Act
        await _sut.ApplyMoodAsync(highPriorityMood);
        await _sut.ApplyMoodAsync(lowPriorityMood);

        // Assert
        _mockConnection.Verify(x => x.SendCommandAsync(It.IsAny<string>()), Times.Once);
        _sut.CurrentMood!.Mood.Should().Be(MoodType.Happy); // Still happy, not tired
    }

    [Fact]
    public async Task ApplyMoodAsync_WithPosition_ShouldSendPositionCommand()
    {
        // Arrange
        var moodState = new MoodState(
            MoodType.Default,
            new Priority(5),
            position: PositionType.North);

        _mockCommandBuilder
            .Setup(x => x.BuildMoodCommand(It.IsAny<MoodState>()))
            .Returns("MOOD:MOOD_DEFAULT:5");

        _mockCommandBuilder
            .Setup(x => x.BuildPositionCommand(PositionType.North, 5))
            .Returns("POS:N:5");

        // Act
        await _sut.ApplyMoodAsync(moodState);

        // Assert
        _mockConnection.Verify(x => x.SendCommandAsync("MOOD:MOOD_DEFAULT:5"), Times.Once);
        _mockConnection.Verify(x => x.SendCommandAsync("POS:N:5"), Times.Once);
    }

    [Fact]
    public async Task ApplyMoodAsync_WithAnimation_ShouldSendAnimationCommand()
    {
        // Arrange
        var moodState = new MoodState(
            MoodType.Happy,
            new Priority(8),
            animation: AnimationType.Laugh);

        _mockCommandBuilder
            .Setup(x => x.BuildMoodCommand(It.IsAny<MoodState>()))
            .Returns("MOOD:MOOD_HAPPY:8");

        _mockCommandBuilder
            .Setup(x => x.BuildAnimationCommand(AnimationType.Laugh))
            .Returns("ANIM:LAUGH");

        // Act
        await _sut.ApplyMoodAsync(moodState);

        // Assert
        _mockConnection.Verify(x => x.SendCommandAsync("ANIM:LAUGH"), Times.Once);
    }

    [Fact]
    public async Task CheckExpiryAsync_WhenMoodExpired_ShouldResetToBaseline()
    {
        // Arrange
        var expiredMood = new MoodState(
            MoodType.Happy,
            new Priority(5),
            duration: Duration.FromSeconds(1)); // 1 second duration

        _mockCommandBuilder
            .Setup(x => x.BuildMoodCommand(It.IsAny<MoodState>()))
            .Returns("MOOD:MOOD_HAPPY:5");

        _mockCommandBuilder
            .Setup(x => x.BuildResetCommand())
            .Returns("RESET");

        await _sut.ApplyMoodAsync(expiredMood);

        // Wait for mood to expire
        await Task.Delay(1500);

        // Act
        await _sut.CheckExpiryAsync();

        // Assert
        _mockConnection.Verify(x => x.SendCommandAsync("RESET"), Times.Once);
        _sut.CurrentMood!.Mood.Should().Be(MoodType.Default);
        _sut.CurrentMood.Priority.Value.Should().Be(0);
    }

    [Fact]
    public async Task ResetAsync_ShouldSendResetCommandAndClearState()
    {
        // Arrange
        var moodState = new MoodState(MoodType.Happy, new Priority(5));

        _mockCommandBuilder
            .Setup(x => x.BuildMoodCommand(It.IsAny<MoodState>()))
            .Returns("MOOD:MOOD_HAPPY:5");

        _mockCommandBuilder
            .Setup(x => x.BuildResetCommand())
            .Returns("RESET");

        await _sut.ApplyMoodAsync(moodState);

        // Act
        await _sut.ResetAsync();

        // Assert
        _mockConnection.Verify(x => x.SendCommandAsync("RESET"), Times.Once);
        _sut.CurrentMood!.Mood.Should().Be(MoodType.Default);
        _sut.CurrentMood.Priority.Value.Should().Be(0);
    }
}
