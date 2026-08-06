using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Catalog & Listing API", "Manages catalog categories, attributes, units, brands, conditions, listing drafts, validation, moderation, and publication workflows. Owns catalog taxonomy and listing state transitions; does not own search index infrastructure directly.", "1.0.0", "Catalog & Listing Core Team", "Seller portals, marketplace frontends, moderation back-office");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.CatalogListing.Contracts.SystemVersionResponse("emcore-catalog-listing-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
