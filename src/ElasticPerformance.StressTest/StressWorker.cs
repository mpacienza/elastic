using System.Diagnostics;
using ElasticPerformance.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ElasticPerformance.StressTest;

public class StressWorker : BackgroundService
{
    private readonly ILogger<StressWorker> _logger;
    private readonly ICuentaBancariaRepository _repository;
    private readonly StressTestConfig _config;

    public StressWorker(
        ILogger<StressWorker> logger,
        ICuentaBancariaRepository repository,
        IOptions<StressTestConfig> config)
    {
        _logger     = logger;
        _repository = repository;
        _config     = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Elasticsearch Stress Test ===");
        _logger.LogInformation("Workers: {W} | Duración: {D}s | Warmup: {WU}s",
            _config.ConcurrentWorkers, _config.DurationSeconds, _config.WarmupSeconds);

        var count = await _repository.CountAsync();
        if (count == 0)
        {
            _logger.LogWarning("El índice está vacío. Insertando 50.000 documentos de prueba...");
            var seed = CuentaBancariaFactory.Generate(50_000);
            await _repository.CreateManyAsync(seed);
            _logger.LogInformation("Datos insertados.");
        }
        else
        {
            _logger.LogInformation("Documentos en índice: {Count:N0}", count);
        }

        var pool = new DataPool();
        await pool.LoadAsync(_repository, _logger);

        if (!pool.HasData)
        {
            _logger.LogError("No se pudo cargar el pool de datos. Abortando.");
            return;
        }

        if (_config.WarmupSeconds > 0)
        {
            _logger.LogInformation("Warmup por {W}s...", _config.WarmupSeconds);
            using var warmupCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            warmupCts.CancelAfter(TimeSpan.FromSeconds(_config.WarmupSeconds));
            await RunWorkersAsync(pool, new StressMetrics(), warmupCts.Token);
            _logger.LogInformation("Warmup completado.");
        }

        _logger.LogInformation("Iniciando stress test...");
        var metrics = new StressMetrics();
        using var testCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        testCts.CancelAfter(TimeSpan.FromSeconds(_config.DurationSeconds));

        var reportTask = ReportProgressAsync(metrics, testCts.Token);
        var sw = Stopwatch.StartNew();
        await RunWorkersAsync(pool, metrics, testCts.Token);
        sw.Stop();

        await reportTask;
        PrintFinalReport(metrics, sw.Elapsed.TotalSeconds);
        Environment.Exit(0);
    }

    private Task RunWorkersAsync(DataPool pool, StressMetrics metrics, CancellationToken ct)
    {
        var tasks = Enumerable.Range(0, _config.ConcurrentWorkers)
            .Select(i => RunSingleWorkerAsync(i, pool, metrics, ct));
        return Task.WhenAll(tasks);
    }

    private async Task RunSingleWorkerAsync(int workerId, DataPool pool, StressMetrics metrics, CancellationToken ct)
    {
        var rng = new Random(workerId * 31 + Environment.TickCount);

        while (!ct.IsCancellationRequested)
        {
            var op = PickOperation(rng);
            var sw = Stopwatch.StartNew();
            bool success = true;

            try
            {
                switch (op)
                {
                    case "FindFirstByCbu":
                        await _repository.FindFirstByCbuAsync(pool.Cbus[rng.Next(pool.Cbus.Length)]);
                        break;
                    case "FindFirstByAlias":
                        await _repository.FindFirstByAliasAsync(pool.Aliases[rng.Next(pool.Aliases.Length)]);
                        break;
                    case "SearchByCbu":
                        await _repository.SearchAsync(cbuCbu: pool.Cbus[rng.Next(pool.Cbus.Length)]);
                        break;
                    case "SearchByAlias":
                        await _repository.SearchAsync(aliAlias: pool.Aliases[rng.Next(pool.Aliases.Length)]);
                        break;
                    case "SearchByBanco":
                        await _repository.SearchAsync(bcoCod: pool.BancoCodes[rng.Next(pool.BancoCodes.Length)]);
                        break;
                    case "Count":
                        await _repository.CountAsync();
                        break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                success = false;
                _logger.LogDebug("Error en worker {Id} op {Op}: {Msg}", workerId, op, ex.Message);
            }

            sw.Stop();
            metrics.Record(op, sw.ElapsedMilliseconds, success);
        }
    }

    private string PickOperation(Random rng)
    {
        var roll = rng.Next(100);
        if (roll < _config.FindFirstByCbuPercent) return "FindFirstByCbu";
        roll -= _config.FindFirstByCbuPercent;
        if (roll < _config.FindFirstByAliasPercent) return "FindFirstByAlias";
        roll -= _config.FindFirstByAliasPercent;
        if (roll < _config.SearchByCbuPercent) return "SearchByCbu";
        roll -= _config.SearchByCbuPercent;
        if (roll < _config.SearchByAliasPercent) return "SearchByAlias";
        roll -= _config.SearchByAliasPercent;
        if (roll < _config.SearchByBancoPercent) return "SearchByBanco";
        return "Count";
    }

    private async Task ReportProgressAsync(StressMetrics metrics, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                var s = metrics.GetSummary(sw.Elapsed.TotalSeconds);
                _logger.LogInformation(
                    "[{T:F0}s] Req: {R:N0} | RPS: {RPS:F1} | Avg: {Avg:F1}ms | P95: {P95}ms | Errors: {E}",
                    sw.Elapsed.TotalSeconds, s.TotalRequests, s.RequestsPerSec, s.AvgMs, s.P95Ms, s.TotalErrors);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void PrintFinalReport(StressMetrics metrics, double elapsedSeconds)
    {
        var s = metrics.GetSummary(elapsedSeconds);

        _logger.LogInformation("");
        _logger.LogInformation("╔══════════════════════════════════════════╗");
        _logger.LogInformation("║      STRESS TEST - RESULTADO FINAL       ║");
        _logger.LogInformation("╠══════════════════════════════════════════╣");
        _logger.LogInformation("║ Duración real  : {V,8:F2} s               ║", elapsedSeconds);
        _logger.LogInformation("║ Total requests : {V,8:N0}                 ║", s.TotalRequests);
        _logger.LogInformation("║ Errores        : {V,8:N0}                 ║", s.TotalErrors);
        _logger.LogInformation("║ RPS            : {V,8:F1}                 ║", s.RequestsPerSec);
        _logger.LogInformation("╠══════════════════════════════════════════╣");
        _logger.LogInformation("║ Latencia (ms)                            ║");
        _logger.LogInformation("║   Min  : {V,8}                           ║", s.MinMs);
        _logger.LogInformation("║   Avg  : {V,8:F1}                         ║", s.AvgMs);
        _logger.LogInformation("║   P50  : {V,8}                           ║", s.P50Ms);
        _logger.LogInformation("║   P95  : {V,8}                           ║", s.P95Ms);
        _logger.LogInformation("║   P99  : {V,8}                           ║", s.P99Ms);
        _logger.LogInformation("║   Max  : {V,8}                           ║", s.MaxMs);
        _logger.LogInformation("╠══════════════════════════════════════════╣");
        _logger.LogInformation("║ Por operación                            ║");
        foreach (var op in s.ByOperation.OrderByDescending(x => x.Value))
        {
            var errors = s.ErrorsByOperation.GetValueOrDefault(op.Key, 0);
            _logger.LogInformation("║  {Op,-16}: {V,6:N0} req  {E,4} err       ║", op.Key, op.Value, errors);
        }
        _logger.LogInformation("╚══════════════════════════════════════════╝");
    }
}
