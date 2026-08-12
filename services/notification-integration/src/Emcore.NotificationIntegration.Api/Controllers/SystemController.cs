using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace Emcore.NotificationIntegration.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public class SystemController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public SystemController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(new Emcore.NotificationIntegration.Contracts.SystemVersionResponse("emcore-notification-integration-api", "0.1.0", _env.EnvironmentName));
    }
}