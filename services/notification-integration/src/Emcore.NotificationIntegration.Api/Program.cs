using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Notification & Integration API", "Manages the central multi-channel notification inbox, dynamic notification templates, transactional delivery (email, SMS, push), recipient preferences, incoming provider webhooks, customer outbound webhooks, delivery failure auditing, and event replay.", "1.0.0", "Notification & Integration Core Team", "Messaging dispatchers, end-user inbox clients, external webhook integrations");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.NotificationIntegration.Contracts.SystemVersionResponse("emcore-notification-integration-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
