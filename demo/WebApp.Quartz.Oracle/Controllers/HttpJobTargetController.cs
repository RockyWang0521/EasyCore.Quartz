using Microsoft.AspNetCore.Mvc;
using WebApp.Quartz.Oracle.Dto;

namespace WebApp.Quartz.Oracle.Controllers;

[ApiController]
[Route("demo")]
public sealed class HttpJobTargetController : ControllerBase
{
    private readonly ILogger<HttpJobTargetController> _logger;

    public HttpJobTargetController(ILogger<HttpJobTargetController> logger)
    {
        _logger = logger;
    }

    [HttpGet("ping")]
    public IActionResult Get()
    {
        _logger.LogInformation("HTTP job GET ping called");
        return Ok(new { ok = true, at = DateTimeOffset.UtcNow });
    }

    [HttpPost("ping")]
    public IActionResult Post(PostDto dto)
    {
        _logger.LogInformation("HTTP job POST ping called -- {Id} -- {Title}", dto.Id, dto.Title);
        return Ok(new { ok = true, dto.Id, dto.Title });
    }
}
