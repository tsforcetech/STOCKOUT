using Emcore.IdentityAccess.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/identity/admin/users")]
[ApiController]
public sealed class AdminController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public AdminController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Administrative user account status modification
    /// </summary>
    [HttpPost("status")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> AdminUpdateUserStatusPostAsync([FromBody] AdminUpdateUserStatusRequest req, CancellationToken ct)
    {
        return MapResult(await _service.AdminUpdateUserStatusAsync(req, UserId, ct));
    }

    /// <summary>
    /// Administrative user status modification by ID
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> AdminUpdateUserStatusPutAsync([FromRoute] string id, [FromBody] AdminUpdateUserStatusRequest req, CancellationToken ct)
    {
        return MapResult(await _service.AdminUpdateUserStatusAsync(req with { UserId = id }, UserId, ct));
    }
}
