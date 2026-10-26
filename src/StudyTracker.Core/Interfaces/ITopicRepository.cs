using StudyTracker.Core.Models;

namespace StudyTracker.Core.Interfaces;

public interface ITopicRepository
{
    Task<IEnumerable<Topic>> GetAllAsync();

    Task<Topic?> GetByIdAsync(int id);

    Task<int> CreateAsync(Topic topic);

    Task<bool> UpdateAsync(Topic topic);

    Task<bool> SetStatusAsync(int id, string status);

    Task<IEnumerable<TopicSummaryDto>> GetSummariesAsync();
}
