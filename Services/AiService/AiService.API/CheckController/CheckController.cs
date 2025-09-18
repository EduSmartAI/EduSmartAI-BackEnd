using Microsoft.AspNetCore.Mvc;

namespace AiService.API.CheckController;

[ApiController]
[Route("api/v1/[controller]")]
public class CheckController : ControllerBase
{
    [HttpGet("health")]
    public async Task<IActionResult> Check()
    {
        return Ok("Ai Service is up and running");
    }
}