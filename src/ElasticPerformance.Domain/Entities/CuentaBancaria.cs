namespace ElasticPerformance.Domain.Entities;

public class CuentaBancaria
{
    public string? Id { get; set; }
    public string CbuCbu { get; set; } = string.Empty;
    public string AliAlias { get; set; } = string.Empty;
    public string BcoCod { get; set; } = string.Empty;
    public string AhId { get; set; } = string.Empty;
    public string AhName { get; set; } = string.Empty;
    public Banco Banco { get; set; } = new();
    public Alias Alias { get; set; } = new();
    public TipoAhorrista AhType { get; set; } = new();
    public TipoCuenta AccType { get; set; } = new();
    public Moneda Currency { get; set; } = new();
    public string FilId { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public DateTime FechaModificacion { get; set; }
}
