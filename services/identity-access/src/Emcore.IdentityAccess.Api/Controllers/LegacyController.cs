using System;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/identity")]
[ApiController]
[ApiExplorerSettings(GroupName = "Legacy Compatibility")]
public sealed class LegacyController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public LegacyController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Legacy identity registration alias
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> LegacyRegisterAsync([FromBody] RegisterRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RegisterAsync(req, ct));
    }

    /// <summary>
    /// Legacy identity verification alias
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> LegacyVerifyAsync([FromBody] VerifyRequest req, CancellationToken ct)
    {
        if (req.Channel?.Equals("Mobile", StringComparison.OrdinalIgnoreCase) == true)
        {
            return MapResult(await _service.ConfirmMobileVerificationAsync(new ConfirmMobileVerificationRequest(req.UserId, req.Token), ct));
        }
        return MapResult(await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest(req.UserId, req.Token), ct));
    }

    /// <summary>
    /// Legacy resend verification alias
    /// </summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> LegacyResendVerificationAsync([FromBody] ResendVerificationRequest req, CancellationToken ct)
    {
        if (req.Channel?.Equals("Mobile", StringComparison.OrdinalIgnoreCase) == true)
        {
            return MapResult(await _service.SendMobileVerificationAsync(new SendMobileVerificationRequest(req.UserId), ct));
        }
        return MapResult(await _service.SendEmailVerificationAsync(new SendEmailVerificationRequest(req.UserId), ct));
    }

    /// <summary>
    /// Legacy login authentication alias
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> LegacyLoginAsync([FromBody] LoginRequest req, CancellationToken ct)
    {
        return MapResult(await _service.LoginAsync(req, ct));
    }

    /// <summary>
    /// Legacy token refresh alias
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> LegacyRefreshAsync([FromBody] RefreshRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RefreshAsync(req, ct));
    }

    /// <summary>
    /// Legacy session logout alias
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    public async Task<IActionResult> LegacyLogoutAsync(CancellationToken ct)
    {
        return MapResult(await _service.LogoutAsync(null, UserId, ct));
    }

    /// <summary>
    /// Retrieve user profile by explicit ID
    /// </summary>
    [HttpGet("users/{id}")]
    [ProducesResponseType(typeof(CurrentIdentityResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> LegacyGetUserByIdAsync([FromRoute] string id, CancellationToken ct)
    {
        return MapResult(await _service.GetCurrentIdentityAsync(id, ct));
    }
}
