using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Inventory & Media API", "Manages real-time physical inventory, stock adjustments, order reservations, secure media upload sessions, digital assets, documents, and signed downloads. Owns inventory counts and asset storage metadata; does not own marketplace listing taxonomy.", "1.0.0", "Inventory & Media Core Team", "Seller applications, logistics integrations, media storage pipelines");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.InventoryMedia.Contracts.SystemVersionResponse("emcore-inventory-media-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
