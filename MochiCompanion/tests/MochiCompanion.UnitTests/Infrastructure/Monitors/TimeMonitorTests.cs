using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MochiCompanion.Domain.Enums;
using MochiCompanion.Infrastructure.Monitors;

namespace MochiCompanion.UnitTests.Infrastructure.Monitors;

public class TimeMonitorTests
{
    private readonly Mock<ILogger<TimeMonitor>> _mockLogger;
    private readonly TimeMonitor _sut;

    public TimeMonitorTests()
    {
        _mockLogger = new Mock<ILogger<TimeMonitor>>();
        _sut = new TimeMonitor(_mockLogger.Object);
    }

    [Fact]
    public void Name_ShouldReturnTimeMonitor()
    {
        // Assert
        _sut.Name.Should().Be("TimeMonitor");
    }

    [Fact]
    public void CheckInterval_ShouldBeOneMinute()
    {
        // Assert
        _sut.CheckInterval.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CheckAsync_ShouldReturnMoodSuggestion()
    {
        // Act
        var result = await _sut.CheckAsync();

        // Assert
        result.Should().NotBeNull();
        result.MonitorName.Should().Be("TimeMonitor");
        result.HasSuggestion.Should().BeTrue();
        result.SuggestedMood.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckAsync_CalledTwiceInSameHour_ShouldReturnNoChangeOnSecondCall()
    {
        // Act
        var result1 = await _sut.CheckAsync();
        var result2 = await _sut.CheckAsync();

        // Assert
        result1.HasSuggestion.Should().BeTrue();
        result2.HasSuggestion.Should().BeFalse(); // No change in same hour
    }

    [Fact]
    public async Task CheckAsync_ShouldHavePriorityBetween1And3()
    {
        // Act
        var result = await _sut.CheckAsync();

        // Assert
        result.SuggestedMood!.Priority.Value.Should().BeInRange(1, 3);
    }

    [Fact]
    public async Task CheckAsync_ShouldSuggestValidMood()
    {
        // Act
        var result = await _sut.CheckAsync();

        // Assert
        result.SuggestedMood!.Mood.Should().BeOneOf(
            MoodType.Default,
            MoodType.Happy,
            MoodType.Tired);
    }
}
