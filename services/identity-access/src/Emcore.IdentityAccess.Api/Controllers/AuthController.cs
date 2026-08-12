using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Api.Controllers;

[Route("api/v1/auth")]
[ApiController]
public sealed class AuthController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public AuthController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Register new user identity
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    [ProducesResponseType(typeof(ProblemDetails), 422)]
    public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RegisterAsync(req, ct));
    }

    /// <summary>
    /// Authenticate user credentials
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest req, CancellationToken ct)
    {
        return MapResult(await _service.LoginAsync(req, ct));
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("token/refresh")]
    [ProducesResponseType(typeof(RefreshResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RefreshAsync(req, ct));
    }

    /// <summary>
    /// Terminate current session
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest? req, CancellationToken ct)
    {
        return MapResult(await _service.LogoutAsync(req?.RefreshToken, UserId, ct));
    }

    /// <summary>
    /// Revoke all active sessions
    /// </summary>
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> LogoutAllAsync(CancellationToken ct)
    {
        return MapResult(await _service.LogoutAllAsync(UserId, ct));
    }

    /// <summary>
    /// List active authentication sessions
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<SessionDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> GetSessionsAsync(CancellationToken ct)
    {
        return MapResult(await _service.GetSessionsAsync(UserId, ct));
    }

    /// <summary>
    /// Revoke specific authentication session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> RevokeSessionAsync([FromRoute] string sessionId, CancellationToken ct)
    {
        return MapResult(await _service.RevokeSessionAsync(UserId, sessionId, ct));
    }
}
