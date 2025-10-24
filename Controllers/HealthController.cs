using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobStream.Api.Data;

namespace JobStream.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly JobStreamDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(JobStreamDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "JobStream.Api"
        });
    }

    /// <summary>
    /// Detailed health check including database connectivity
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailed()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "JobStream.Api",
            checks = new Dictionary<string, object>()
        };

        try
        {
            // Check database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync();
            health.checks.Add("database", new
            {
                status = canConnect ? "healthy" : "unhealthy",
                responseTime = "< 1s"
            });

            if (!canConnect)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    checks = health.checks
                });
            }

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}
