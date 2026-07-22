using System.Collections.Concurrent;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

public class LogAggregator
{
    private readonly ConcurrentDictionary<string, Queue<LogEntry>> _buffers = new();
    private readonly int _maxLines;

    public LogAggregator(int maxLines = 1000) => _maxLines = maxLines;

    public void Append(string serviceId, string line, bool isError = false)
    {
        var buf = _buffers.GetOrAdd(serviceId, _ => new Queue<LogEntry>());
        lock (buf)
        {
            buf.Enqueue(new LogEntry(serviceId, line, isError, DateTime.UtcNow));
            while (buf.Count > _maxLines) buf.Dequeue();
        }
    }

    public IReadOnlyList<LogEntry> GetLines(string serviceId, int count = 200)
    {
        if (!_buffers.TryGetValue(serviceId, out var buf)) return [];
        lock (buf) return buf.TakeLast(count).ToList();
    }

    public IReadOnlyList<LogEntry> GetAllLines(int count = 500)
    {
        return _buffers.Values
            .SelectMany(buf => { lock (buf) return buf.ToList(); })
            .OrderBy(e => e.Timestamp)
            .TakeLast(count)
            .ToList();
    }
}

public record LogEntry(string ServiceId, string Message, bool IsError, DateTime Timestamp);
