namespace ElasticPerformance.StressTest;

public class StressTestConfig
{
    public int ConcurrentWorkers { get; set; } = 10;
    public int DurationSeconds { get; set; } = 30;
    public int WarmupSeconds { get; set; } = 3;

    public int FindFirstByCbuPercent { get; set; } = 50;
    public int FindFirstByAliasPercent { get; set; } = 40;
    public int SearchByCbuPercent { get; set; } = 0;
    public int SearchByAliasPercent { get; set; } = 0;
    public int SearchByBancoPercent { get; set; } = 0;
    public int CountPercent { get; set; } = 10;
}
