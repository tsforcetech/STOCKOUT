using Emcore.IdentityAccess.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Microsoft.AspNetCore.Mvc;
using Emcore.BuildingBlocks.Core;
using Microsoft.AspNetCore.Authorization;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/auth/verification")]
[ApiController]
[AllowAnonymous]
public sealed class VerificationController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public VerificationController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Send email verification challenge
    /// </summary>
    [HttpPost("email/send")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> SendEmailVerificationAsync([FromBody] SendEmailVerificationRequest req, CancellationToken ct)
    {
        return MapResult(await _service.SendEmailVerificationAsync(req, ct));
    }

    /// <summary>
    /// Confirm email verification token
    /// </summary>
    [HttpPost("email/confirm")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> ConfirmEmailVerificationAsync([FromBody] ConfirmEmailVerificationRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ConfirmEmailVerificationAsync(req, ct));
    }

    /// <summary>
    /// Send mobile verification OTP
    /// </summary>
    [HttpPost("mobile/send")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> SendMobileVerificationAsync([FromBody] SendMobileVerificationRequest req, CancellationToken ct)
    {
        return MapResult(await _service.SendMobileVerificationAsync(req, ct));
    }

    /// <summary>
    /// Confirm mobile verification OTP
    /// </summary>
    [HttpPost("mobile/confirm")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> ConfirmMobileVerificationAsync([FromBody] ConfirmMobileVerificationRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ConfirmMobileVerificationAsync(req, ct));
    }
}
