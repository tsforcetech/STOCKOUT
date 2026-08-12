using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Conversation & Realtime API", "Manages secure inter-organizational messaging conversations, thread participants, encrypted chat messages, file attachments, read receipt tracking, communication blocking, realtime tokens, and synchronized live cursors. For full SignalR hub events and transport specifications, refer to REALTIME_EVENT_CONTRACT_REFERENCE.md.", "1.0.0", "Conversation & Realtime Core Team", "Chat widgets, mobile realtime consumers, communication moderators");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));


app.MapControllers();

app.Run();

public partial class Program { }
