using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Emcore.BuildingBlocks.Core;
using Microsoft.AspNetCore.Authorization;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/auth/password")]
[ApiController]
[Authorize]
public sealed class PasswordController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public PasswordController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Initiate password reset
    /// </summary>
    [HttpPost("forgot")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ForgotPasswordResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ForgotPasswordAsync(req, ct));
    }

    /// <summary>
    /// Complete password reset
    /// </summary>
    [HttpPost("reset")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResetPasswordResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ResetPasswordAsync(req, ct));
    }

    /// <summary>
    /// Change authenticated user password
    /// </summary>
    [HttpPost("change")]
    [ProducesResponseType(typeof(ChangePasswordResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ChangePasswordAsync(UserId, req, ct));
    }
}
