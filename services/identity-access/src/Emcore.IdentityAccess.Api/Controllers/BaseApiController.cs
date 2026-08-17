using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.BuildingBlocks.Core;
using Emcore.BuildingBlocks.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Emcore.BuildingBlocks.Security;

namespace Emcore.IdentityAccess.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected internal IActionResult MapResult<T>(AppResult<T> res)
    {
        if (!res.IsSuccess)
        {
            return Problem(
                statusCode: res.StatusCode,
                title: res.ErrorTitle,
                detail: res.ErrorDetail,
                type: $"https://emcore.platform/errors/{res.StatusCode}");
        }

        if (res.StatusCode == 201)
        {
            return StatusCode(201, res.Data);
        }

        return Ok(res.Data);
    }

    protected string UserId => HttpContext.RequestServices.GetRequiredService<ICurrentUser>().UserId ?? string.Empty;
    protected string? SessionId => HttpContext.RequestServices.GetRequiredService<ICurrentUser>().SessionId;
}
