using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE User & Organization API", "Manages user profiles, organizations, multi-branch hierarchies, operational teams, member invitations, granular roles, permissions, and organization settings. Owns organizational structure and authorization attributes; does not own authentication or cryptographic credentials.", "1.0.0", "User & Organization Core Team", "Platform clients, BFFs, and partner applications");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.UserOrganization.Contracts.SystemVersionResponse("emcore-user-organization-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
