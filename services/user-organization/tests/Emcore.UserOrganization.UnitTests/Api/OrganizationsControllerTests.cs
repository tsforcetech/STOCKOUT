using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Emcore.UserOrganization.Api.Controllers;
using Emcore.UserOrganization.Application.Organizations;
using Emcore.UserOrganization.Contracts.Organizations;
using Emcore.BuildingBlocks.Security;

namespace Emcore.UserOrganization.UnitTests.Api;

public class OrganizationsControllerTests
{
    [Fact]
    public async Task CreateOrganization_WithoutUserContext_ReturnsUnauthorized()
    {
        var mockService = new Mock<IOrganizationService>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        
        mockCurrentUser.Setup(c => c.UserId).Returns((string?)null);

        var controller = new OrganizationsController(mockService.Object, mockCurrentUser.Object);

        var request = new CreateOrganizationRequest
        {
            EntityType = 1,
            DisplayName = "Test",
            Capabilities = new System.Collections.Generic.List<int> { 1 }
        };

        var result = await controller.CreateOrganization(request);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task CreateOrganization_WithUserContext_CallsServiceWithUserId()
    {
        var mockService = new Mock<IOrganizationService>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        
        var userId = "user-123";
        mockCurrentUser.Setup(c => c.UserId).Returns(userId);

        mockService.Setup(s => s.CreateOrganizationAsync(userId, It.IsAny<CreateOrganizationRequest>()))
            .ReturnsAsync(new OrganizationResponse { Id = "org-1" });

        var controller = new OrganizationsController(mockService.Object, mockCurrentUser.Object);

        var request = new CreateOrganizationRequest
        {
            EntityType = 1,
            DisplayName = "Test",
            Capabilities = new System.Collections.Generic.List<int> { 1 }
        };

        var result = await controller.CreateOrganization(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<OrganizationResponse>(createdResult.Value);
        Assert.Equal("org-1", response.Id);

        mockService.Verify(s => s.CreateOrganizationAsync(userId, request), Times.Once);
    }
}
