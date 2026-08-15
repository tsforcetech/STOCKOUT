using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Emcore.BuildingBlocks.Core;
using Microsoft.AspNetCore.Authorization;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/auth")]
[ApiController]
[Authorize]
public sealed class MfaController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public MfaController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Verify multi-factor challenge during login
    /// </summary>
    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> VerifyMfaLoginAsync([FromBody] MfaLoginVerifyRequest req, CancellationToken ct)
    {
        return MapResult(await _service.VerifyMfaLoginAsync(req, ct));
    }

    /// <summary>
    /// Initialize MFA authenticator registration
    /// </summary>
    [HttpPost("mfa/register")]
    [ProducesResponseType(typeof(RegisterMfaResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> RegisterMfaAsync([FromBody] RegisterMfaRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RegisterMfaAsync(UserId, req, ct));
    }

    /// <summary>
    /// Confirm MFA authenticator registration
    /// </summary>
    [HttpPost("mfa/confirm")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> ConfirmMfaAsync([FromBody] ConfirmMfaRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ConfirmMfaAsync(UserId, req, ct));
    }

    /// <summary>
    /// Resend MFA OTP
    /// </summary>
    [HttpPost("mfa/resend")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResendMfaResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> ResendMfaAsync([FromBody] ResendMfaRequest req, CancellationToken ct)
    {
        return MapResult(await _service.ResendMfaAsync(req.UserId, req, ct));
    }

    /// <summary>
    /// Initiate step-up authorization challenge
    /// </summary>
    [HttpPost("stepup/initiate")]
    [ProducesResponseType(typeof(InitiateStepUpResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> InitiateStepUpAsync([FromBody] InitiateStepUpRequest req, CancellationToken ct)
    {
        return MapResult(await _service.InitiateStepUpAsync(UserId, req, ct));
    }

    /// <summary>
    /// Verify step-up authorization challenge
    /// </summary>
    [HttpPost("stepup/verify")]
    [ProducesResponseType(typeof(VerifyStepUpResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> VerifyStepUpAsync([FromBody] VerifyStepUpRequest req, CancellationToken ct)
    {
        return MapResult(await _service.VerifyStepUpAsync(UserId, req, ct));
    }
}
