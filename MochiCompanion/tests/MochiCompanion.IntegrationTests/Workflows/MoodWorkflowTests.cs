using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MochiCompanion.Application.Interfaces.ICommunication;
using MochiCompanion.Application.Services;
using MochiCompanion.Domain.Entities;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Domain.ValueObjects;
using MochiCompanion.Infrastructure.Communication;
using MochiCompanion.IntegrationTests.TestHelpers;

namespace MochiCompanion.IntegrationTests.Workflows;

/// <summary>
/// End-to-end tests for mood workflow from suggestion to command sending.
/// </summary>
public class MoodWorkflowTests
{
    [Fact]
    public async Task MoodWorkflow_ApplyHighPriorityMood_ShouldSendCorrectCommands()
    {
        // Arrange
        var mockConnection = new MockMochiConnection();
        await mockConnection.ConnectAsync("TEST", 115200);

        var commandBuilder = new CommandBuilder();
        var moodService = new MoodService(
            mockConnection,
            commandBuilder,
            NullLogger<MoodService>.Instance);

        var moodState = new MoodState(
            MoodType.Happy,
            Priority.Event(8),
            position: PositionType.North,
            animation: AnimationType.Laugh);

        // Act
        await moodService.ApplyMoodAsync(moodState);

        // Assert
        mockConnection.SentCommands.Should().HaveCount(3);
        mockConnection.SentCommands[0].Should().Be("MOOD:MOOD_HAPPY:8");
        mockConnection.SentCommands[1].Should().Be("POS:N:8");
        mockConnection.SentCommands[2].Should().Be("ANIM:LAUGH");
    }

    [Fact]
    public async Task MoodWorkflow_PriorityOverride_LowerPriorityShouldBeIgnored()
    {
        // Arrange
        var mockConnection = new MockMochiConnection();
        await mockConnection.ConnectAsync("TEST", 115200);

        var commandBuilder = new CommandBuilder();
        var moodService = new MoodService(
            mockConnection,
            commandBuilder,
            NullLogger<MoodService>.Instance);

        var highPriorityMood = new MoodState(MoodType.Happy, Priority.Event(8));
        var lowPriorityMood = new MoodState(MoodType.Tired, Priority.TimeBased(2));

        // Act
        await moodService.ApplyMoodAsync(highPriorityMood);
        mockConnection.ClearCommands();
        await moodService.ApplyMoodAsync(lowPriorityMood);

        // Assert
        mockConnection.SentCommands.Should().BeEmpty(); // Low priority was ignored
        moodService.CurrentMood!.Mood.Should().Be(MoodType.Happy); // Still happy
    }

    [Fact]
    public async Task MoodWorkflow_TimedMood_ShouldExpireAndReset()
    {
        // Arrange
        var mockConnection = new MockMochiConnection();
        await mockConnection.ConnectAsync("TEST", 115200);

        var commandBuilder = new CommandBuilder();
        var moodService = new MoodService(
            mockConnection,
            commandBuilder,
            NullLogger<MoodService>.Instance);

        var timedMood = new MoodState(
            MoodType.Happy,
            Priority.Event(8),
            duration: Duration.FromSeconds(1)); // 1 second

        // Act
        await moodService.ApplyMoodAsync(timedMood);
        mockConnection.ClearCommands();

        // Wait for expiry
        await Task.Delay(1500);

        await moodService.CheckExpiryAsync();

        // Assert
        mockConnection.SentCommands.Should().Contain("RESET");
        moodService.CurrentMood!.Mood.Should().Be(MoodType.Default);
        moodService.CurrentMood.Priority.Value.Should().Be(0);
    }

    [Fact]
    public async Task MoodWorkflow_ResetCommand_ShouldClearStateAndSendReset()
    {
        // Arrange
        var mockConnection = new MockMochiConnection();
        await mockConnection.ConnectAsync("TEST", 115200);

        var commandBuilder = new CommandBuilder();
        var moodService = new MoodService(
            mockConnection,
            commandBuilder,
            NullLogger<MoodService>.Instance);

        var moodState = new MoodState(MoodType.Angry, Priority.Event(9));

        // Act
        await moodService.ApplyMoodAsync(moodState);
        mockConnection.ClearCommands();
        await moodService.ResetAsync();

        // Assert
        mockConnection.SentCommands.Should().Contain("RESET");
        moodService.CurrentMood!.Mood.Should().Be(MoodType.Default);
    }

    [Fact]
    public void CommandBuilder_GeneratesCorrectProtocolCommands()
    {
        // Arrange
        var commandBuilder = new CommandBuilder();

        // Act & Assert - verify all command types
        commandBuilder.BuildResetCommand().Should().Be("RESET");
        commandBuilder.BuildIdleCommand(true).Should().Be("IDLE:ON");
        commandBuilder.BuildIdleCommand(false).Should().Be("IDLE:OFF");
        commandBuilder.BuildBlinkCommand(true).Should().Be("BLINK:ON");
        commandBuilder.BuildBlinkCommand(false).Should().Be("BLINK:OFF");
        commandBuilder.BuildAnimationCommand(AnimationType.Confused).Should().Be("ANIM:CONFUSED");
        commandBuilder.BuildPositionCommand(PositionType.East, 5).Should().Be("POS:E:5");
    }
}
