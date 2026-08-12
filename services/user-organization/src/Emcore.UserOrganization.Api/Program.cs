using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE User & Organization API", "Manages user profiles, organizations, multi-branch hierarchies, operational teams, member invitations, granular roles, permissions, and organization settings. Owns organizational structure and authorization attributes; does not own authentication or cryptographic credentials.", "1.0.0", "User & Organization Core Team", "Platform clients, BFFs, and partner applications");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));


app.MapControllers();

app.Run();

public partial class Program { }
