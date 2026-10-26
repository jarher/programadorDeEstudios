using StudyTracker.Core.Models;

namespace StudyTracker.Core.Interfaces;

public interface ISessionRepository
{
    Task<int> RecordSessionAsync(StudySession session);

    Task<IEnumerable<StudySession>> GetByTopicIdAsync(int topicId);

    Task<IEnumerable<DailyActivityDto>> GetDailyActivityAsync(DateTime fromDate, DateTime toDate);
}
