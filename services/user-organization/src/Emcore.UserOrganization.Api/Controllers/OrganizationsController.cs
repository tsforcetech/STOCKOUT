using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Emcore.UserOrganization.Contracts.Organizations;
using Emcore.UserOrganization.Application.Organizations;
using System;
using Microsoft.AspNetCore.Authorization;
using Emcore.BuildingBlocks.Security;

namespace Emcore.UserOrganization.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ICurrentUser _currentUser;

    public OrganizationsController(IOrganizationService organizationService, ICurrentUser currentUser)
    {
        _organizationService = organizationService;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            var ownerUserId = _currentUser.UserId;
            if (string.IsNullOrEmpty(ownerUserId))
            {
                return Unauthorized(new { Error = "User context missing" });
            }

            var response = await _organizationService.CreateOrganizationAsync(ownerUserId, request);
            return CreatedAtAction(nameof(GetOrganization), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrganization(string id)
    {
        var response = await _organizationService.GetOrganizationAsync(id);
        if (response == null) return NotFound();

        return Ok(response);
    }
}
