using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.BuildingBlocks.Core;
using Emcore.BuildingBlocks.Api;
using Microsoft.AspNetCore.Mvc;

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

    protected string UserId => ExtractUserId(HttpContext);

    private string ExtractUserId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-User-Id", out var uid) && !string.IsNullOrWhiteSpace(uid))
            return uid.ToString();

        var sub = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(sub)) return sub;

        // Check authorization header for JWT sub claim fallback without dependency on bearer middleware
        if (context.Request.Headers.TryGetValue("Authorization", out var auth) && auth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var parts = auth.ToString().Substring("Bearer ".Length).Trim().Split('.');
                if (parts.Length >= 2)
                {
                    var payloadJson = Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/'));
                    using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                    if (doc.RootElement.TryGetProperty("sub", out var subProp))
                        return subProp.GetString() ?? string.Empty;
                }
            }
            catch { }
        }

        // Default simulation fallback ID for local tests
        return "user_1234567890_default";
    }
}
