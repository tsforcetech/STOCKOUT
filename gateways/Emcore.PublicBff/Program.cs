using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Public BFF API", "Backend-For-Frontend gateway orchestrating aggregated public marketplace catalog, search discovery, user onboarding, and unauthenticated buyer navigation workflows. Aggregates internal calls into optimized client DTOs.", "1.0.0", "Platform Edge & BFF Team", "Public web frontends, mobile buyer applications, external partner integrations");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new { ServiceName = "Emcore.PublicBff", Version = "0.1.0", Environment = builder.Environment.EnvironmentName }));

app.Run();

public partial class Program { }
