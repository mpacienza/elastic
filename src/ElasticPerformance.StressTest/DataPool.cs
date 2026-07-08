using ElasticPerformance.Application.Interfaces;

namespace ElasticPerformance.StressTest;

public class DataPool
{
    public string[] Cbus { get; private set; } = [];
    public string[] Aliases { get; private set; } = [];
    public string[] BancoCodes { get; private set; } = [];

    public async Task LoadAsync(ICuentaBancariaRepository repository, ILogger logger)
    {
        logger.LogInformation("Cargando pool de datos para consultas...");

        var sample = await repository.SearchAsync();

        Cbus       = sample.Where(x => x.CbuCbu   != null).Select(x => x.CbuCbu).Distinct().Take(500).ToArray();
        Aliases    = sample.Where(x => x.AliAlias  != null).Select(x => x.AliAlias).Distinct().Take(500).ToArray();
        BancoCodes = sample.Where(x => x.BcoCod    != null).Select(x => x.BcoCod).Distinct().Take(100).ToArray();

        logger.LogInformation("Pool cargado: {Cbus} CBUs, {Aliases} aliases, {Bancos} bancos",
            Cbus.Length, Aliases.Length, BancoCodes.Length);
    }

    public bool HasData => Cbus.Length > 0;
}
