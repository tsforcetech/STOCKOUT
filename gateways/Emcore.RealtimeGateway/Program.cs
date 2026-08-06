using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Realtime Gateway API", "Dedicated edge connection point for bidirectional realtime communications, WebSocket transport management, event broadcast federation, live conversation messaging, and dynamic marketplace notifications. For SignalR hub endpoints and event payload schemas, refer to REALTIME_EVENT_CONTRACT_REFERENCE.md.", "1.0.0", "Realtime Edge Infrastructure Team", "SignalR clients, live interactive marketplace widgets, notification subscribers");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new { ServiceName = "Emcore.RealtimeGateway", Version = "0.1.0", Environment = builder.Environment.EnvironmentName }));

app.Run();

public partial class Program { }
