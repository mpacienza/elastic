namespace ElasticPerformance.Application.DTOs;

public class PerformanceResultDto<T>
{
    public T? Result { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public int RecordCount { get; set; }
    public string Operation { get; set; } = string.Empty;
}
