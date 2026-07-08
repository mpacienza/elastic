namespace ElasticPerformance.Infrastructure.Configuration;

public class ElasticsearchSettings
{
    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "cuentas_bancarias";
}
