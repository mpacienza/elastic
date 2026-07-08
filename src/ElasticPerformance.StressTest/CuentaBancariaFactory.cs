using ElasticPerformance.Domain.Entities;

namespace ElasticPerformance.StressTest;

public static class CuentaBancariaFactory
{
    private static readonly string[] BancoCodes = ["001", "011", "014", "017", "020", "027", "034", "044", "060", "072"];
    private static readonly string[] AhTypes    = ["F", "J"];
    private static readonly string[] AccTypes   = ["CA", "CC"];
    private static readonly string[] Currencies = ["ARS", "USD"];
    private static readonly string[] States     = ["ACTIVO", "INACTIVO"];

    public static IEnumerable<CuentaBancaria> Generate(int count)
    {
        var rng = new Random(42);
        var now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            var bancoCode = BancoCodes[rng.Next(BancoCodes.Length)];
            var cbu       = GenerateCbu(rng, bancoCode);
            var alias     = $"{RandomWord(rng)}.{RandomWord(rng)}.{RandomWord(rng)}";
            var ahId      = rng.NextInt64(10_000_000, 99_999_999).ToString();

            yield return new CuentaBancaria
            {
                CbuCbu   = cbu,
                AliAlias = alias,
                BcoCod   = bancoCode,
                AhId     = ahId,
                AhName   = $"TITULAR {i:D6}",
                FilId    = $"SUC{rng.Next(1, 999):D3}",
                FechaAlta = now.AddDays(-rng.Next(1, 3650)),
                FechaModificacion = now.AddDays(-rng.Next(0, 365)),
                Banco = new Banco
                {
                    BcoNombre    = $"BANCO {bancoCode}",
                    BcoHabilitado = 1,
                    Cuit         = $"30{rng.NextInt64(100_000_000, 999_999_999):D9}0"
                },
                Alias = new Alias
                {
                    AliAliasUser   = alias,
                    AliAddDt       = now.AddDays(-rng.Next(1, 1000)),
                    AliActiveAlias = 1,
                    AliState       = States[rng.Next(States.Length)],
                    AliSecNum      = rng.Next(1, 9999),
                    AliInternalId  = rng.Next(1, 999999)
                },
                AhType = new TipoAhorrista
                {
                    Codigo      = AhTypes[rng.Next(AhTypes.Length)],
                    Descripcion = "PERSONA"
                },
                AccType = new TipoCuenta
                {
                    TcaCodigo      = AccTypes[rng.Next(AccTypes.Length)],
                    TcaDescripcion = "CUENTA"
                },
                Currency = new Moneda
                {
                    TmoCodigo      = Currencies[rng.Next(Currencies.Length)],
                    TmoDescripcion = "MONEDA"
                }
            };
        }
    }

    private static string GenerateCbu(Random rng, string bancoCode)
    {
        var sucursal = rng.Next(1, 9999).ToString("D4");
        var cuenta   = rng.NextInt64(1_000_000_000_000L, 9_999_999_999_999L).ToString();
        return $"{bancoCode.PadLeft(3, '0')}0{sucursal}0{cuenta}";
    }

    private static readonly string[] Words =
        ["sol", "luna", "mar", "rio", "pez", "ave", "roca", "viento", "fuego", "agua",
         "campo", "monte", "flor", "hoja", "rama", "nube", "cielo", "tierra", "noche", "dia"];

    private static string RandomWord(Random rng) => Words[rng.Next(Words.Length)];
}
