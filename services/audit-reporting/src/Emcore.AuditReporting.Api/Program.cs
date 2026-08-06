using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Audit & Reporting API", "Provides tamper-evident audit log search, immutable audit transaction details, background report generation jobs, real-time executive dashboards, operational metric exports, and regulatory compliance data exports.", "1.0.0", "Audit & Reporting Core Team", "Compliance officers, internal security evaluators, audit export workers");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.AuditReporting.Contracts.SystemVersionResponse("emcore-audit-reporting-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
