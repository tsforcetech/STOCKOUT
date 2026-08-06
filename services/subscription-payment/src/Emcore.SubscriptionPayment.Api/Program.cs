using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Subscription & Payment API", "Manages monetization plans, tenant entitlements, active subscriptions, metered usage tracking, payment gateway intents, asynchronous webhooks, automated refunds, tax invoicing, payment receipts, and settlement reconciliation.", "1.0.0", "Subscription & Payment Core Team", "Billing portals, payment gateway webhooks, financial reconciliation pipelines");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new Emcore.SubscriptionPayment.Contracts.SystemVersionResponse("emcore-subscription-payment-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();

public partial class Program { }
