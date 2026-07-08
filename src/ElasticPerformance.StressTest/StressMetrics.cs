using System.Collections.Concurrent;

namespace ElasticPerformance.StressTest;

public class StressMetrics
{
    private long _totalRequests;
    private long _totalErrors;
    private readonly ConcurrentBag<long> _latencies = new();
    private readonly ConcurrentDictionary<string, long> _requestsByOperation = new();
    private readonly ConcurrentDictionary<string, long> _errorsByOperation = new();

    public void Record(string operation, long elapsedMs, bool success)
    {
        Interlocked.Increment(ref _totalRequests);
        _latencies.Add(elapsedMs);
        _requestsByOperation.AddOrUpdate(operation, 1, (_, v) => v + 1);

        if (!success)
        {
            Interlocked.Increment(ref _totalErrors);
            _errorsByOperation.AddOrUpdate(operation, 1, (_, v) => v + 1);
        }
    }

    public StressSummary GetSummary(double elapsedSeconds)
    {
        var sorted = _latencies.OrderBy(x => x).ToArray();
        return new StressSummary
        {
            TotalRequests    = _totalRequests,
            TotalErrors      = _totalErrors,
            ElapsedSeconds   = elapsedSeconds,
            RequestsPerSec   = elapsedSeconds > 0 ? _totalRequests / elapsedSeconds : 0,
            MinMs            = sorted.Length > 0 ? sorted[0] : 0,
            MaxMs            = sorted.Length > 0 ? sorted[^1] : 0,
            AvgMs            = sorted.Length > 0 ? sorted.Average() : 0,
            P50Ms            = Percentile(sorted, 50),
            P95Ms            = Percentile(sorted, 95),
            P99Ms            = Percentile(sorted, 99),
            ByOperation      = _requestsByOperation.ToDictionary(k => k.Key, v => v.Value),
            ErrorsByOperation= _errorsByOperation.ToDictionary(k => k.Key, v => v.Value),
        };
    }

    private static long Percentile(long[] sorted, int percentile)
    {
        if (sorted.Length == 0) return 0;
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }
}

public class StressSummary
{
    public long TotalRequests { get; set; }
    public long TotalErrors { get; set; }
    public double ElapsedSeconds { get; set; }
    public double RequestsPerSec { get; set; }
    public long MinMs { get; set; }
    public long MaxMs { get; set; }
    public double AvgMs { get; set; }
    public long P50Ms { get; set; }
    public long P95Ms { get; set; }
    public long P99Ms { get; set; }
    public Dictionary<string, long> ByOperation { get; set; } = new();
    public Dictionary<string, long> ErrorsByOperation { get; set; } = new();
}
