using Microsoft.Extensions.Time.Testing;
using StudyTracker.Core.Services;

namespace StudyTracker.Core.Tests;

public class TimerServiceTests
{
    [Fact]
    public void Start_InitializesCountdown()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);

        timer.Start(TimeSpan.FromMinutes(5));

        Assert.True(timer.IsRunning);
        Assert.False(timer.IsPaused);
        Assert.Equal(TimeSpan.FromMinutes(5), timer.RemainingTime);
        Assert.Equal(TimeSpan.Zero, timer.ElapsedTime);
    }

    [Fact]
    public void Advance_UpdatesElapsedAndRemaining()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);

        timer.Start(TimeSpan.FromMinutes(5));
        time.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(TimeSpan.FromMinutes(2), timer.ElapsedTime);
        Assert.Equal(TimeSpan.FromMinutes(3), timer.RemainingTime);
    }

    [Fact]
    public void ReachingZero_RaisesCompletedOnce()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);
        var completed = 0;
        TimeSpan? lastTick = null;

        timer.SessionCompleted += (_, _) => completed++;
        timer.Tick += (_, remaining) => lastTick = remaining;

        timer.Start(TimeSpan.FromMinutes(5));
        time.Advance(TimeSpan.FromMinutes(6));

        Assert.False(timer.IsRunning);
        Assert.False(timer.IsPaused);
        Assert.Equal(TimeSpan.FromMinutes(5), timer.ElapsedTime);
        Assert.Equal(TimeSpan.Zero, timer.RemainingTime);
        Assert.Equal(1, completed);
        Assert.Equal(TimeSpan.Zero, lastTick);
    }

    [Fact]
    public void Pause_FreezesEffectiveTime_AndResumeContinues()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);

        timer.Start(TimeSpan.FromMinutes(10));
        time.Advance(TimeSpan.FromSeconds(150));
        timer.Pause();

        Assert.True(timer.IsRunning);
        Assert.True(timer.IsPaused);
        Assert.Equal(TimeSpan.FromSeconds(150), timer.ElapsedTime);
        Assert.Equal(TimeSpan.FromSeconds(450), timer.RemainingTime);

        time.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromSeconds(150), timer.ElapsedTime);
        Assert.Equal(TimeSpan.FromSeconds(450), timer.RemainingTime);

        timer.Resume();
        time.Advance(TimeSpan.FromMinutes(1));

        Assert.True(timer.IsRunning);
        Assert.False(timer.IsPaused);
        Assert.Equal(TimeSpan.FromSeconds(210), timer.ElapsedTime);
        Assert.Equal(TimeSpan.FromSeconds(390), timer.RemainingTime);
    }

    [Fact]
    public void Stop_PreservesEffectiveTime_AndPreventsCompletion()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);
        var completed = 0;
        TimeSpan? lastTick = null;

        timer.SessionCompleted += (_, _) => completed++;
        timer.Tick += (_, remaining) => lastTick = remaining;

        timer.Start(TimeSpan.FromMinutes(10));
        time.Advance(TimeSpan.FromMinutes(3));
        timer.Stop();

        Assert.False(timer.IsRunning);
        Assert.False(timer.IsPaused);
        Assert.Equal(TimeSpan.FromMinutes(3), timer.ElapsedTime);
        Assert.Equal(TimeSpan.FromMinutes(7), timer.RemainingTime);
        Assert.Equal(TimeSpan.FromMinutes(7), lastTick);
        Assert.Equal(0, completed);

        time.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromMinutes(3), timer.ElapsedTime);
        Assert.Equal(0, completed);
    }

    [Fact]
    public void Start_WhileRunning_ResetsCountdown()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);
        var completed = 0;

        timer.SessionCompleted += (_, _) => completed++;

        timer.Start(TimeSpan.FromMinutes(10));
        time.Advance(TimeSpan.FromMinutes(2));
        timer.Start(TimeSpan.FromMinutes(3));

        Assert.True(timer.IsRunning);
        Assert.False(timer.IsPaused);
        Assert.Equal(TimeSpan.Zero, timer.ElapsedTime);
        Assert.Equal(TimeSpan.FromMinutes(3), timer.RemainingTime);
        Assert.Equal(0, completed);
    }

    [Fact]
    public void Stop_WhenIdle_DoesNothing()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);

        timer.Stop();

        Assert.False(timer.IsRunning);
        Assert.False(timer.IsPaused);
        Assert.Equal(TimeSpan.Zero, timer.ElapsedTime);
        Assert.Equal(TimeSpan.Zero, timer.RemainingTime);
    }

    [Fact]
    public void Tick_ReportsRemainingTime()
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);
        var ticks = new List<TimeSpan>();

        timer.Tick += (_, remaining) => ticks.Add(remaining);

        timer.Start(TimeSpan.FromMinutes(1));
        time.Advance(TimeSpan.FromSeconds(1));

        Assert.NotEmpty(ticks);
        Assert.Equal(TimeSpan.FromMinutes(1), ticks[0]);
        Assert.Equal(TimeSpan.FromSeconds(59), ticks[^1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Start_WithNonPositiveDuration_Throws(int seconds)
    {
        var time = new FakeTimeProvider();
        using var timer = new TimerService(time);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => timer.Start(TimeSpan.FromSeconds(seconds)));
    }
}
