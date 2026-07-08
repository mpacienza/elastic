using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ElasticPerformance.Application.Interfaces;
using ElasticPerformance.Domain.Entities;
using ElasticPerformance.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticPerformance.Infrastructure.Repositories;

public class CuentaBancariaRepository : ICuentaBancariaRepository
{
    private readonly ElasticsearchClient _client;
    private readonly string _index;
    private readonly ILogger<CuentaBancariaRepository> _logger;

    public CuentaBancariaRepository(IOptions<ElasticsearchSettings> settings, ILogger<CuentaBancariaRepository> logger)
    {
        _logger = logger;
        var cfg = settings.Value;
        var esSettings = new ElasticsearchClientSettings(new Uri(cfg.Uri))
            .DefaultIndex(cfg.IndexName);

        _client = new ElasticsearchClient(esSettings);
        _index  = cfg.IndexName;

        EnsureIndexAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(_index);
        if (exists.Exists) return;

        await _client.Indices.CreateAsync(_index, c => c
            .Mappings(m => m
                .Properties<CuentaBancaria>(p => p
                    .Keyword(f => f.CbuCbu)
                    .Keyword(f => f.AliAlias)
                    .Keyword(f => f.BcoCod)
                    .Keyword(f => f.AhId)
                    .Keyword(f => f.AhName)
                    .Keyword(f => f.FilId)
                    .Date(f => f.FechaAlta)
                    .Date(f => f.FechaModificacion)
                )
            )
        );
    }

    public async Task<long> CreateManyAsync(IEnumerable<CuentaBancaria> cuentas)
    {
        var list = cuentas.ToList();
        if (list.Count == 0) return 0;

        const int batchSize = 1000;
        long total = 0;

        for (int i = 0; i < list.Count; i += batchSize)
        {
            var batch = list.Skip(i).Take(batchSize);
            var response = await _client.BulkAsync(b => b
                .Index(_index)
                .IndexMany(batch));

            if (response.IsValidResponse)
                total += response.Items.Count;
        }

        // Forzar refresh para que los documentos sean visibles inmediatamente
        await _client.Indices.RefreshAsync(_index);

        return total;
    }

    public async Task<List<CuentaBancaria>> SearchAsync(
        string? cbuCbu   = null,
        string? aliAlias = null,
        string? bcoCod   = null,
        string? ahId     = null)
    {
        var filters = new List<Query>();

        if (!string.IsNullOrWhiteSpace(cbuCbu))
            filters.Add(new TermQuery(Infer.Field<CuentaBancaria>(f => f.CbuCbu)) { Value = cbuCbu });
        if (!string.IsNullOrWhiteSpace(aliAlias))
            filters.Add(new TermQuery(Infer.Field<CuentaBancaria>(f => f.AliAlias)) { Value = aliAlias });
        if (!string.IsNullOrWhiteSpace(bcoCod))
            filters.Add(new TermQuery(Infer.Field<CuentaBancaria>(f => f.BcoCod)) { Value = bcoCod });
        if (!string.IsNullOrWhiteSpace(ahId))
            filters.Add(new TermQuery(Infer.Field<CuentaBancaria>(f => f.AhId)) { Value = ahId });

        SearchResponse<CuentaBancaria> response;

        if (filters.Count > 0)
        {
            response = await _client.SearchAsync<CuentaBancaria>(s => s
                .Indices(_index)
                .Query(q => q.Bool(b => b.Filter(filters.ToArray())))
                .Size(1000));
        }
        else
        {
            response = await _client.SearchAsync<CuentaBancaria>(s => s
                .Indices(_index)
                .Query(q => q.MatchAll(new MatchAllQuery()))
                .Size(1000));
        }

        if (!response.IsValidResponse)
        {
            _logger.LogError("SearchAsync falló: {Error}", response.ElasticsearchServerError?.Error?.Reason ?? response.DebugInformation);
            return [];
        }

        return response.Documents.ToList();
    }

    public async Task<CuentaBancaria?> FindFirstByCbuAsync(string cbu)
    {
        var response = await _client.SearchAsync<CuentaBancaria>(s => s
            .Indices(_index)
            .Query(q => q.Term(t => t
                .Field(Infer.Field<CuentaBancaria>(f => f.CbuCbu))
                .Value(cbu)))
            .Size(1));

        return response.IsValidResponse ? response.Documents.FirstOrDefault() : null;
    }

    public async Task<CuentaBancaria?> FindFirstByAliasAsync(string alias)
    {
        var response = await _client.SearchAsync<CuentaBancaria>(s => s
            .Indices(_index)
            .Query(q => q.Term(t => t
                .Field(Infer.Field<CuentaBancaria>(f => f.AliAlias))
                .Value(alias)))
            .Size(1));

        return response.IsValidResponse ? response.Documents.FirstOrDefault() : null;
    }

    public async Task<long> CountAsync()
    {
        var response = await _client.CountAsync(c => c.Indices(_index));
        return response.IsValidResponse ? response.Count : 0;
    }

    public async Task DeleteAllAsync()
    {
        await _client.DeleteByQueryAsync<CuentaBancaria>(_index, d => d
            .Query(q => q.MatchAll(new MatchAllQuery())));
    }
}
