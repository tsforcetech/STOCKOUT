using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Emcore.UserOrganization.Contracts.Organizations;
using Emcore.UserOrganization.Application.Organizations;
using System;

namespace Emcore.UserOrganization.Api.Controllers;

[ApiController]
[Route("api/v1/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            // In a real app, this would come from the JWT claims or context
            var ownerUserId = Guid.NewGuid().ToString(); 
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
