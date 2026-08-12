using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Bidding & Deal API", "Manages interactive marketplace bidding, seller offers, buyer counteroffers, formal bid acceptance, deal creation, deal lifecycle progress, cancellations, and transaction completion. Owns negotiation workflows and agreement state preservation.", "1.0.0", "Bidding & Deal Core Team", "Buyer/seller marketplace frontends, negotiation bots, escrow integrations");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));


app.MapControllers();

app.Run();

public partial class Program { }
