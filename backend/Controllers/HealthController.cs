using Microsoft.AspNetCore.Mvc;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("health")]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Returns the API health status.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "stock-analyzer-api",
        timestamp = DateTimeOffset.UtcNow
    });
}
