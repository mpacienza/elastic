using ElasticPerformance.Domain.Entities;

namespace ElasticPerformance.Application.Interfaces;

public interface ICuentaBancariaRepository
{
    Task<long> CreateManyAsync(IEnumerable<CuentaBancaria> cuentas);
    Task<List<CuentaBancaria>> SearchAsync(string? cbuCbu = null, string? aliAlias = null, string? bcoCod = null, string? ahId = null);
    Task<CuentaBancaria?> FindFirstByCbuAsync(string cbu);
    Task<CuentaBancaria?> FindFirstByAliasAsync(string alias);
    Task<long> CountAsync();
    Task DeleteAllAsync();
}
