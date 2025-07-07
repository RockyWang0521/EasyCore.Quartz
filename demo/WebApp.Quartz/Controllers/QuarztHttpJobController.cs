using Microsoft.AspNetCore.Mvc;
using WebApp.Quartz.Dto;

namespace WebApp.Quartz.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuarztHttpJobController : ControllerBase
    {
        private readonly ILogger<QuarztHttpJobController> _logger;

        public QuarztHttpJobController(ILogger<QuarztHttpJobController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Get")]
        public async Task Get()
        {
            // Do something here

            _logger.LogInformation("Get called");

            await Task.CompletedTask;
        }

        [HttpPost("Post")]
        public async Task Post(PostDto dto)
        {
            // Do something here

            _logger.LogInformation($"Post called -- {dto.Id}--{dto.Title}");

            await Task.CompletedTask;
        }
    }
}
