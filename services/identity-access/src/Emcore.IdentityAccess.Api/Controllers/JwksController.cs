using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.BuildingBlocks.Core;
using Emcore.BuildingBlocks.Api;
using Microsoft.AspNetCore.Mvc;
using Emcore.IdentityAccess.Application.Abstractions;

namespace Emcore.IdentityAccess.Api.Controllers;

[ApiController]
public sealed class JwksController : ControllerBase
{
    private readonly IJwksService _jwks;

    public JwksController(IJwksService jwks)
    {
        _jwks = jwks;
    }

    /// <summary>
    /// Retrieve public JSON Web Key Set (JWKS)
    /// </summary>
    [HttpGet("/.well-known/jwks.json")]
    [Microsoft.AspNetCore.Http.Tags("Public Security Metadata")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetPublicJwks()
    {
        return Content(_jwks.GetJwksJson(), "application/json");
    }

    /// <summary>
    /// Retrieve versioned public JWKS under auth prefix
    /// </summary>
    [HttpGet("/api/v1/auth/.well-known/jwks.json")]
    [Microsoft.AspNetCore.Http.Tags("Public Security Metadata")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetAuthJwks()
    {
        return Content(_jwks.GetJwksJson(), "application/json");
    }
}
