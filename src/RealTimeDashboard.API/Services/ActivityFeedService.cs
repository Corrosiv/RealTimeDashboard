namespace RealTimeDashboard.API.Services;

using System.Collections.Concurrent;

public class ActivityFeedService
{
    private readonly ConcurrentQueue<string> _events = new();

    public void Publish(string message) => _events.Enqueue(message);
    public IEnumerable<string> GetRecent(int limit = 50) => _events.Reverse().Take(limit);
}
