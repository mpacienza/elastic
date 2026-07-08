using ElasticPerformance.Application.DTOs;
using ElasticPerformance.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElasticPerformance.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CuentaBancariaController : ControllerBase
{
    private readonly CuentaBancariaService _service;
    private readonly ILogger<CuentaBancariaController> _logger;

    public CuentaBancariaController(CuentaBancariaService service, ILogger<CuentaBancariaController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    [HttpPost("massive")]
    public async Task<ActionResult<PerformanceResultDto<long>>> CreateMassiveData([FromQuery] int count = 10000)
    {
        if (count <= 0 || count > 1000000)
            return BadRequest("El count debe estar entre 1 y 1,000,000");

        _logger.LogInformation("Iniciando creación masiva de {Count} registros", count);
        var result = await _service.CreateMassiveDataAsync(count);
        _logger.LogInformation("Creación masiva completada: {Count} registros en {Ms}ms", result.RecordCount, result.ElapsedMilliseconds);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PerformanceResultDto<List<ElasticPerformance.Domain.Entities.CuentaBancaria>>>> Search(
        [FromQuery] string? cbuCbu   = null,
        [FromQuery] string? aliAlias = null,
        [FromQuery] string? bcoCod   = null,
        [FromQuery] string? ahId     = null)
    {
        var criteria = new SearchCriteriaDto { CbuCbu = cbuCbu, AliAlias = aliAlias, BcoCod = bcoCod, AhId = ahId };
        _logger.LogInformation("Búsqueda con criterios: {@Criteria}", criteria);
        var result = await _service.SearchAsync(criteria);
        _logger.LogInformation("Búsqueda completada: {Count} resultados en {Ms}ms", result.RecordCount, result.ElapsedMilliseconds);
        return Ok(result);
    }

    [HttpGet("count")]
    public async Task<ActionResult<long>> GetCount()
    {
        var count = await _service.GetCountAsync();
        return Ok(new { count });
    }

    [HttpDelete("all")]
    public async Task<ActionResult> DeleteAll()
    {
        _logger.LogWarning("Eliminando todos los documentos del índice");
        await _service.DeleteAllAsync();
        return Ok(new { message = "Todos los documentos han sido eliminados" });
    }
}
