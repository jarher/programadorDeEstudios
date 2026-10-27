using StudyTracker.Core.Models;

namespace StudyTracker.Core.Interfaces;

public interface IStatsService
{
    Task<StudyStatisticsDto> GetStatisticsAsync(DateTime? asOfDate = null);
}
