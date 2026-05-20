using System.Collections.Concurrent;

namespace BankTransferMVC.Services;

/// <summary>
/// Captures every Clear Junction interaction (simulator or live) for diagnostic display.
/// Bounded ring buffer (default 100 entries) so memory stays small even under load.
/// </summary>
public sealed record CjCallEntry(
    DateTimeOffset At,
    string Mode,                 // "Simulation" / "Live"
    string Method,
    string Path,
    int? StatusCode,
    long ElapsedMs,
    string? RequestSummary,
    string? ResponseSummary,
    string? Error);

public interface ICjCallLog
{
    void Record(CjCallEntry entry);
    IReadOnlyList<CjCallEntry> Recent(int max = 50);
    void Clear();
}

public class CjCallLog : ICjCallLog
{
    private readonly ConcurrentQueue<CjCallEntry> _items = new();
    private const int Capacity = 100;

    public void Record(CjCallEntry entry)
    {
        _items.Enqueue(entry);
        while (_items.Count > Capacity && _items.TryDequeue(out _)) { }
    }

    public IReadOnlyList<CjCallEntry> Recent(int max = 50) =>
        _items.Reverse().Take(max).ToList();

    public void Clear()
    {
        while (_items.TryDequeue(out _)) { }
    }
}
