using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Api.Controllers;

[ApiController]
public sealed class ServiceClientController : BaseApiController
{
    private readonly IdentityApplicationService _service;

    public ServiceClientController(IdentityApplicationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Issue OAuth2 client credentials token
    /// </summary>
    [HttpPost("api/v1/auth/token")]
    [ProducesResponseType(typeof(ServiceTokenResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> IssueServiceTokenAsync([FromBody] ServiceTokenRequest req, CancellationToken ct)
    {
        return MapResult(await _service.IssueServiceTokenAsync(req, ct));
    }

    /// <summary>
    /// Register new service workload client
    /// </summary>
    [HttpPost("api/v1/identity/service-clients/register")]
    [ProducesResponseType(typeof(RegisterServiceClientResponse), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> RegisterServiceClientAsync([FromBody] RegisterServiceClientRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RegisterServiceClientAsync(req, ct));
    }

    /// <summary>
    /// Rotate service client secret credential
    /// </summary>
    [HttpPost("api/v1/identity/service-clients/{id}/rotate")]
    [ProducesResponseType(typeof(RotateServiceClientCredentialResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> RotateServiceClientCredentialAsync([FromRoute] string id, [FromBody] RotateServiceClientCredentialRequest? req, CancellationToken ct)
    {
        return MapResult(await _service.RotateServiceClientCredentialAsync(req ?? new RotateServiceClientCredentialRequest(id), ct));
    }

    /// <summary>
    /// Revoke specific service client secret
    /// </summary>
    [HttpPost("api/v1/identity/service-clients/credentials/revoke")]
    [ProducesResponseType(typeof(StandardSuccessResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> RevokeServiceClientCredentialAsync([FromBody] RevokeServiceClientCredentialRequest req, CancellationToken ct)
    {
        return MapResult(await _service.RevokeServiceClientCredentialAsync(req, ct));
    }

    /// <summary>
    /// List active credentials for service client
    /// </summary>
    [HttpGet("api/v1/identity/service-clients/{id}/credentials")]
    [ProducesResponseType(typeof(List<ServiceClientCredentialDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> ListServiceClientCredentialsAsync([FromRoute] string id, CancellationToken ct)
    {
        return MapResult(await _service.ListServiceClientCredentialsAsync(id, ct));
    }
}
