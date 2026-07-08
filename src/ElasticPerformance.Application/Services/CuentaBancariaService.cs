using System.Diagnostics;
using ElasticPerformance.Application.DTOs;
using ElasticPerformance.Application.Interfaces;
using ElasticPerformance.Domain.Entities;

namespace ElasticPerformance.Application.Services;

public class CuentaBancariaService
{
    private readonly ICuentaBancariaRepository _repository;
    private static readonly Random _random = new();

    private static readonly string[] Bancos = {
        "INDUSTRIAL AND COMMERCIAL BANK OF CHINA",
        "BANCO DE LA NACION ARGENTINA",
        "BANCO PROVINCIA DE BUENOS AIRES",
        "BANCO SANTANDER",
        "BANCO GALICIA",
        "BBVA ARGENTINA",
        "BANCO MACRO",
        "BANCO SUPERVIELLE",
        "HSBC BANK ARGENTINA",
        "BANCO PATAGONIA"
    };

    private static readonly string[] CodigosBancos = {
        "015", "011", "014", "072", "007", "017", "285", "027", "150", "034"
    };

    private static readonly string[] Estados = {
        "ALIAS_SOV", "ALIAS_ACTIVE", "ALIAS_PENDING", "ALIAS_BLOCKED"
    };

    public CuentaBancariaService(ICuentaBancariaRepository repository)
    {
        _repository = repository;
    }

    public async Task<PerformanceResultDto<long>> CreateMassiveDataAsync(int count)
    {
        var stopwatch = Stopwatch.StartNew();
        var cuentas = GenerateRandomCuentas(count);
        var inserted = await _repository.CreateManyAsync(cuentas);
        stopwatch.Stop();

        return new PerformanceResultDto<long>
        {
            Result = inserted,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            RecordCount = (int)inserted,
            Operation = $"Insert Massive - {count} records"
        };
    }

    public async Task<PerformanceResultDto<List<CuentaBancaria>>> SearchAsync(SearchCriteriaDto criteria)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = await _repository.SearchAsync(
            criteria.CbuCbu,
            criteria.AliAlias,
            criteria.BcoCod,
            criteria.AhId);
        stopwatch.Stop();

        return new PerformanceResultDto<List<CuentaBancaria>>
        {
            Result = results,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
            RecordCount = results.Count,
            Operation = "Search"
        };
    }

    public async Task<long> GetCountAsync() => await _repository.CountAsync();

    public async Task DeleteAllAsync() => await _repository.DeleteAllAsync();

    private List<CuentaBancaria> GenerateRandomCuentas(int count)
    {
        var cuentas = new List<CuentaBancaria>(count);
        var baseDate = new DateTime(2024, 1, 1);

        for (int i = 0; i < count; i++)
        {
            var bancoIndex = _random.Next(Bancos.Length);
            var alias = GenerateRandomAlias();
            var cbu = GenerateRandomCBU();
            var ahId = GenerateRandomAhId();
            var fechaAlta = baseDate.AddDays(_random.Next(0, 365));

            cuentas.Add(new CuentaBancaria
            {
                CbuCbu = cbu,
                AliAlias = alias,
                BcoCod = CodigosBancos[bancoIndex],
                AhId = ahId,
                AhName = GenerateRandomName(),
                Banco = new Banco
                {
                    BcoNombre = Bancos[bancoIndex],
                    BcoHabilitado = 1,
                    Cuit = GenerateRandomCuit()
                },
                Alias = new Alias
                {
                    AliAliasUser = alias,
                    AliAddDt = fechaAlta,
                    AliActiveAlias = 1,
                    AliState = Estados[_random.Next(Estados.Length)],
                    AliSecNum = _random.Next(0, 100),
                    AliInternalId = _random.Next(1000000, 99999999)
                },
                AhType = new TipoAhorrista
                {
                    Codigo = _random.Next(2) == 0 ? "F" : "J",
                    Descripcion = _random.Next(2) == 0 ? "Persona Física" : "Persona Jurídica"
                },
                AccType = new TipoCuenta
                {
                    TcaCodigo = "10",
                    TcaDescripcion = "CA Pesos"
                },
                Currency = new Moneda
                {
                    TmoCodigo = "032",
                    TmoDescripcion = "Pesos Argentinos"
                },
                FilId = _random.Next(100000, 999999).ToString(),
                FechaAlta = fechaAlta,
                FechaModificacion = fechaAlta.AddMinutes(_random.Next(1, 1000))
            });
        }
        return cuentas;
    }

    private static string GenerateRandomCBU() =>
        new(Enumerable.Range(0, 22).Select(_ => (char)('0' + _random.Next(10))).ToArray());

    private static string GenerateRandomAlias()
    {
        var words = new[] {
            "YERNO", "GAFAS", "RECIO", "FELIZ", "PERRO", "GATO", "MESA", "SILLA",
            "LARGO", "CORTO", "ALTO", "BAJO", "VERDE", "AZUL", "ROJO", "NEGRO"
        };
        return $"{words[_random.Next(words.Length)]}.{words[_random.Next(words.Length)]}.{words[_random.Next(words.Length)]}";
    }

    private static string GenerateRandomAhId() =>
        new(Enumerable.Range(0, 11).Select(_ => (char)('0' + _random.Next(10))).ToArray());

    private static string GenerateRandomName()
    {
        var nombres = new[] { "JUAN", "MARIA", "PEDRO", "LUCIA", "CARLOS", "ANA", "LUIS", "SOFIA" };
        var apellidos = new[] { "PEREZ", "GOMEZ", "RODRIGUEZ", "FERNANDEZ", "LOPEZ", "MARTINEZ", "GARCIA", "GONZALEZ" };
        return $"{apellidos[_random.Next(apellidos.Length)]} {nombres[_random.Next(nombres.Length)]} {apellidos[_random.Next(apellidos.Length)]}";
    }

    private static string GenerateRandomCuit() =>
        new(Enumerable.Range(0, 11).Select(_ => (char)('0' + _random.Next(10))).ToArray());
}
