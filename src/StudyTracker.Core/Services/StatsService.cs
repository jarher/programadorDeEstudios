using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;

namespace StudyTracker.Core.Services;

public sealed class StatsService : IStatsService
{
    private readonly ISessionRepository _sessionRepository;

    public StatsService(ISessionRepository sessionRepository)
    {
        ArgumentNullException.ThrowIfNull(sessionRepository);
        _sessionRepository = sessionRepository;
    }

    public async Task<StudyStatisticsDto> GetStatisticsAsync(DateTime? asOfDate = null)
    {
        var toDate = (asOfDate ?? DateTime.Today).Date;
        var fromDate = toDate.AddMonths(-3);
        var dailyActivity = (await _sessionRepository.GetDailyActivityAsync(fromDate, toDate))
            .ToDictionary(activity => activity.Date.Date);
        var completeActivity = new List<DailyActivityDto>((toDate - fromDate).Days + 1);

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            completeActivity.Add(dailyActivity.TryGetValue(date, out var activity)
                ? activity
                : new DailyActivityDto { Date = date });
        }

        var activeDates = await _sessionRepository.GetActiveStudyDatesAsync(toDate);

        return new StudyStatisticsDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            DailyActivity = completeActivity,
            CurrentStreak = CalculateCurrentStreak(activeDates, toDate)
        };
    }

    private static int CalculateCurrentStreak(IEnumerable<DateTime> activeDates, DateTime toDate)
    {
        using var dates = activeDates
            .Select(date => date.Date)
            .Distinct()
            .OrderByDescending(date => date)
            .GetEnumerator();

        if (!dates.MoveNext())
        {
            return 0;
        }

        var expectedDate = dates.Current == toDate
            ? toDate
            : toDate.AddDays(-1);

        if (dates.Current != expectedDate)
        {
            return 0;
        }

        var streak = 1;

        while (dates.MoveNext())
        {
            expectedDate = expectedDate.AddDays(-1);

            if (dates.Current != expectedDate)
            {
                break;
            }

            streak++;
        }

        return streak;
    }
}
