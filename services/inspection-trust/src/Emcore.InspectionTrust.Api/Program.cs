using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Inspection & Trust API", "Manages physical inspection requests, inspector assignments, facility scheduling, detailed grading checklists, photographic evidence, decisions, inspection certificates, seller verification, and dynamic trust scores. Owns trust certification and grading integrity.", "1.0.0", "Inspection & Trust Core Team", "Inspector field apps, verification officers, marketplace trust indicators");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.InspectionTrust.Contracts.SystemVersionResponse("emcore-inspection-trust-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
