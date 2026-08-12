using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.BuildingBlocks.Core;
using Emcore.BuildingBlocks.Api;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/auth")]
[ApiController]
[Authorize]
public sealed class AccountController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public AccountController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Inspect user account status
    /// </summary>
    [HttpGet("api/v1/auth/account/status")]
    [ProducesResponseType(typeof(AccountStatusResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> GetAccountStatusAsync(CancellationToken ct)
    {
        return MapResult(await _service.GetAccountStatusAsync(UserId, ct));
    }

    /// <summary>
    /// Retrieve profile for authenticated user
    /// </summary>
    [HttpGet("api/v1/identity/me")]
    [ProducesResponseType(typeof(CurrentIdentityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> GetCurrentIdentityAsync(CancellationToken ct)
    {
        return MapResult(await _service.GetCurrentIdentityAsync(UserId, ct));
    }
}
