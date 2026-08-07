using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Emcore.BuildingBlocks.Api;

public static class OpenApiExtensions
{
    public static IServiceCollection AddEmcoreOpenApi(
        this IServiceCollection services,
        string documentName = "v1",
        string serviceTitle = "EMCORE Service API",
        string serviceDescription = "EMCORE Platform API Service",
        string apiVersion = "1.0.0",
        string serviceOwner = "EMCORE Core Architecture Team",
        string intendedConsumers = "Platform clients, BFFs, and partner applications",
        bool isInternal = false)
    {
        services.AddHttpContextAccessor();

        services.AddOpenApi(documentName, options =>
        {
            options.AddEmcoreSwaggerVersioning(documentName, apiVersion);
            options.AddEmcoreSwaggerSecurity(isInternal);
            options.AddEmcoreSwaggerHeaders();
            options.AddEmcoreSwaggerProblemDetails();
            options.AddEmcoreSwaggerExamples();

            options.AddDocumentTransformer((document, context, ct) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = serviceTitle,
                    Description = $"{serviceDescription}\n\n**Service Ownership:** {serviceOwner}\n**Intended Consumers:** {intendedConsumers}\n**Operational Mode:** Primarily synchronous HTTP API; critical domain mutations emit versioned outbox events and may operate under eventual consistency.\n**Confidentiality:** {(isInternal ? "INTERNAL ONLY — STRICTLY CONFIDENTIAL. Do not expose externally." : "Public/Enterprise Consumer Contract.")}",
                    Version = apiVersion,
                    Contact = new OpenApiContact
                    {
                        Name = "EMCORE Architecture & Support Team",
                        Email = "support@emcore.platform",
                        Url = new Uri("https://emcore.platform/support")
                    },
                    TermsOfService = new Uri("https://emcore.platform/terms-of-service")
                };

                var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";
                if (string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase) || isInternal)
                {
                    document.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = $"https://api.emcore.platform/{documentName}", Description = $"EMCORE {envName} Environment Server" }
                    };
                }
                else
                {
                    var httpContextAccessor = context.ApplicationServices.GetService<IHttpContextAccessor>();
                    var httpContext = httpContextAccessor?.HttpContext;
                    string serverUrl = "/";
                    string serverDesc = $"EMCORE {envName} Standalone Service Server";

                    if (httpContext != null && httpContext.Request != null)
                    {
                        var forwardedHost = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
                        var forwardedProto = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpContext.Request.Scheme;

                        if (!string.IsNullOrEmpty(forwardedHost))
                        {
                            serverUrl = $"{forwardedProto}://{forwardedHost}";
                            serverDesc = "EMCORE Central Gateway Ingress (Try-It-Out)";
                        }
                        else if (httpContext.Request.Host.HasValue)
                        {
                            serverUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                            serverDesc = $"EMCORE {envName} Standalone Host Server";
                        }
                    }

                    document.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = serverUrl, Description = serverDesc },
                        new OpenApiServer { Url = $"https://api.emcore.platform/{documentName}", Description = "EMCORE Platform Target Server" }
                    };
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder UseEmcoreOpenApi(this IEndpointRouteBuilder app, string pattern = "/openapi/{documentName}.json", bool enableStandaloneSwaggerUi = true)
    {
        var env = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = app.ServiceProvider.GetService<IConfiguration>();

        var enableInProd = config?.GetValue<bool>("OpenApi:EnableInProduction") ?? config?.GetValue<bool>("Swagger:EnableInProduction") ?? false;
        var enableJsonInProd = enableInProd || (config?.GetValue<bool>("OpenApi:EnableJsonInProduction") ?? false);
        var enableUiInProd = enableInProd || (config?.GetValue<bool>("OpenApi:EnableUiInProduction") ?? false);
        var requireAuthInProd = config?.GetValue<bool>("OpenApi:RequireAuthorizationInProduction") ?? true;
        var requiredPolicy = config?.GetValue<string>("OpenApi:RequiredPolicy") ?? "PlatformSwaggerAdministrator";

        if (env.IsProduction())
        {
            // Explicit protected Production Swagger enablement: BLOCKED UNTIL APPROVED ADMIN POLICY + JWT VALIDATION EXIST
            enableJsonInProd = false;
        }

        if (!env.IsProduction() || enableJsonInProd)
        {
            var endpoint1 = app.MapOpenApi(pattern);
            var endpoint2 = !pattern.Equals("/swagger/{documentName}/swagger.json", StringComparison.OrdinalIgnoreCase)
                ? app.MapOpenApi("/swagger/{documentName}/swagger.json")
                : null;

            if (env.IsProduction() && requireAuthInProd)
            {
                endpoint1.RequireAuthorization(requiredPolicy);
                endpoint2?.RequireAuthorization(requiredPolicy);
            }
        }

        if (env.IsProduction())
        {
            enableUiInProd = false;
        }

        if (!env.IsProduction() || enableUiInProd)
        {
            if (enableStandaloneSwaggerUi && app is IApplicationBuilder appBuilder)
            {
                if (env.IsProduction() && requireAuthInProd)
                {
                    appBuilder.Use(async (context, next) =>
                    {
                        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
                            (!context.User.Identity?.IsAuthenticated ?? true))
                        {
                            context.Response.StatusCode = 401;
                            return;
                        }
                        await next();
                    });
                }

                appBuilder.UseSwaggerUI(options =>
                {
                    options.RoutePrefix = "swagger";
                    options.SwaggerEndpoint("/openapi/v1.json", "EMCORE Service API (v1)");
                    options.DocumentTitle = "EMCORE Standalone Service UI";

                    if (env.IsProduction() && !(config?.GetValue<bool>("OpenApi:EnableTryItOutInProduction") ?? false))
                    {
                        options.SupportedSubmitMethods();
                    }
                });
            }
        }
        return app;
    }

    public static OpenApiOptions AddEmcoreSwaggerSecurity(this OpenApiOptions options, bool isInternal = false)
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes.TryAdd("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "PRODUCTION GATEWAY JWT VALIDATION: NOT IMPLEMENTED. PRODUCTION FALLBACK TO TEST AUTH: PROHIBITED. DEVELOPMENT ONLY token verification. DEFERRED."
            });

            document.Components.SecuritySchemes.TryAdd("ClientCredentials", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "OAuth-style client credentials for service identities. Requires Client ID and Secret to obtain a short-lived bearer token from POST /api/v1/auth/token.",
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri("https://auth.emcore.platform/api/v1/auth/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "api.access", "General API service access" },
                            { "admin.write", "Elevated administrative state modifications" }
                        }
                    }
                }
            });

            document.Components.SecuritySchemes.TryAdd("StepUpToken", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-StepUp-Token",
                Description = "Identity Step-Up Flow: IMPLEMENTED. Downstream Step-Up Enforcement: NOT VERIFIED / NOT IMPLEMENTED."
            });

            document.Components.SecuritySchemes.TryAdd("WebhookHmac", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Signature-256",
                Description = "PLANNED — NOT IMPLEMENTED."
            });

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, ct) =>
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            var hasAllowAnonymous = metadata.Any(m => m is Microsoft.AspNetCore.Authorization.IAllowAnonymous);
            var hasAuthorize = metadata.Any(m => m is Microsoft.AspNetCore.Authorization.IAuthorizeData);

            if (!hasAllowAnonymous && hasAuthorize && !isInternal)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                if (!operation.Security.Any())
                {
                    var req = new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        }] = new List<string>()
                    };
                    operation.Security.Add(req);
                }
            }
            return Task.CompletedTask;
        });

        return options;
    }

    public static OpenApiOptions AddEmcoreSwaggerHeaders(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, ct) =>
        {
            var path = context.Description.RelativePath ?? string.Empty;
            if (path.StartsWith("health", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            operation.Parameters ??= new List<OpenApiParameter>();

            void AddHeaderIfMissing(string name, string description, bool required, string sample)
            {
                if (!operation.Parameters.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.In == ParameterLocation.Header))
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = name,
                        In = ParameterLocation.Header,
                        Required = required,
                        Description = description,
                        Schema = new OpenApiSchema { Type = "string", Default = new OpenApiString(sample) }
                    });
                }
            }

            AddHeaderIfMissing("X-Request-Id", "Unique per-request tracing identifier (ULID or UUID). Max length 64 chars. Propagated through middleware to backend logs.", false, "req_01HPX7K7R5YZ2X90WY");
            AddHeaderIfMissing("X-Correlation-Id", "Distributed transaction correlation identifier across multiple services. Preserved across outbox domain events and message boundaries.", false, "cor_01HPX7K7R5YZ2X90WY");
            AddHeaderIfMissing("X-Client-Version", "Calling consumer version string for diagnostics and feature deprecation telemetry.", false, "1.0.0-build2026");
            AddHeaderIfMissing("Accept-Language", "Preferred locale for user-facing validation and problem details messaging (e.g., en-AE, ar-AE).", false, "en-AE");

            var method = context.Description.HttpMethod ?? string.Empty;
            var isMutation = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase);

            var isAuthOrSessionOp = path.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("logout", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("refresh", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("mfa", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("stepup", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("verification", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("verify", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("health", StringComparison.OrdinalIgnoreCase) ||
                                    path.Contains("system", StringComparison.OrdinalIgnoreCase);

            // Idempotency disabled as NoOp runtime does not enforce it.
            // if (isMutation && !isAuthOrSessionOp)
            // {
            //     AddHeaderIfMissing("X-Idempotency-Key", "Reserved for future idempotency support. The current runtime does not enforce duplicate-request protection, response replay or payload conflict detection.", false, "idmp_01HPX7K7R5YZ2X90WY");
            // }

            if (!isAuthOrSessionOp && (path.Contains("organizations", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("users", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("tenants", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("catalog", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("deals", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("inventory", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("payments", StringComparison.OrdinalIgnoreCase)))
            {
                var tenantDesc = "Client-supplied requested context. Supplying this value does not grant authorization. Runtime membership validation is not currently verified as active. The authorization layer must validate requested context against the authenticated principal before it can be trusted.";
                AddHeaderIfMissing("X-Tenant-Id", tenantDesc, false, "org_01HPX7K7R5YZ2X90WY0002");
                AddHeaderIfMissing("X-Organization-Id", tenantDesc, false, "org_01HPX7K7R5YZ2X90WY0002");
            }

            return Task.CompletedTask;
        });

        return options;
    }

    public static OpenApiOptions AddEmcoreSwaggerProblemDetails(this OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, ct) =>
        {
            if (string.Equals(context.JsonTypeInfo.Type.Name, "ProblemDetails", StringComparison.OrdinalIgnoreCase))
            {
                schema.Description = "RFC 7807 standardized problem details error payload.";
            }
            return Task.CompletedTask;
        });

        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Schemas.TryAdd("EmcoreProblemDetails", new OpenApiSchema
            {
                Type = "object",
                Description = "RFC 7807 standardized problem details error payload.",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["type"] = new OpenApiSchema { Type = "string", Description = "URI reference identifying the problem type.", Example = new OpenApiString("https://emcore.platform/errors/400") },
                    ["title"] = new OpenApiSchema { Type = "string", Description = "Short human-readable summary of the error.", Example = new OpenApiString("Validation failed") },
                    ["status"] = new OpenApiSchema { Type = "integer", Format = "int32", Description = "HTTP status code.", Example = new OpenApiInteger(400) },
                    ["code"] = new OpenApiSchema { Type = "string", Description = "Stable machine-readable error code.", Example = new OpenApiString("VALIDATION_ERROR") },
                    ["detail"] = new OpenApiSchema { Type = "string", Description = "Detailed diagnostic explanation of the error condition.", Example = new OpenApiString("One or more request parameters failed structural validation.") },
                    ["traceId"] = new OpenApiSchema { Type = "string", Description = "Distributed trace identifier.", Example = new OpenApiString("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01") },
                    ["requestId"] = new OpenApiSchema { Type = "string", Description = "Enterprise request tracking identifier.", Example = new OpenApiString("req_01HPX7K7R5YZ2X90WY") },
                    ["correlationId"] = new OpenApiSchema { Type = "string", Description = "Distributed correlation identifier.", Example = new OpenApiString("cor_01HPX7K7R5YZ2X90WY") },
                    ["errors"] = new OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true, Description = "Property-level validation failure details." }
                }
            });

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, ct) =>
        {
            var path = context.Description.RelativePath ?? string.Empty;
            if (path.StartsWith("health", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var problemRef = new OpenApiSchema
            {
                Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "EmcoreProblemDetails" }
            };

            void AddErrorResponse(string status, string title, string code, string detail)
            {
                if (!operation.Responses.ContainsKey(status))
                {
                    var example = new OpenApiObject
                    {
                        ["type"] = new OpenApiString($"https://emcore.platform/errors/{status}"),
                        ["title"] = new OpenApiString(title),
                        ["status"] = new OpenApiInteger(int.Parse(status)),
                        ["code"] = new OpenApiString(code),
                        ["detail"] = new OpenApiString(detail),
                        ["traceId"] = new OpenApiString("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01")
                    };

                    operation.Responses[status] = new OpenApiResponse
                    {
                        Description = title,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/problem+json"] = new OpenApiMediaType { Schema = problemRef, Example = example },
                            ["application/json"] = new OpenApiMediaType { Schema = problemRef, Example = example }
                        }
                    };
                }
            }

            if (!operation.Responses.ContainsKey("500"))
            {
                operation.Responses["500"] = new OpenApiResponse
                {
                    Description = "Internal Server Error (unhandled exception without Problem Details schema formatting)"
                };
            }

            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            var hasAllowAnonymous = metadata.Any(m => m is Microsoft.AspNetCore.Authorization.IAllowAnonymous);
            var hasAuthorize = metadata.Any(m => m is Microsoft.AspNetCore.Authorization.IAuthorizeData);
            var hasRateLimiting = metadata.Any(m => m.GetType().Name.Contains("EnableRateLimiting"));

            if (!hasAllowAnonymous && hasAuthorize)
            {
                AddErrorResponse("401", "Unauthorized", "AUTH_REQUIRED", "Valid authentication token or credentials were not provided.");
                AddErrorResponse("403", "Forbidden", "ACCESS_DENIED", "Authenticated caller lacks sufficient tenant role or delegated permissions for this resource.");
            }

            if (hasRateLimiting)
            {
                AddErrorResponse("429", "Too Many Requests", "RATE_LIMIT_EXCEEDED", "Caller has exceeded permitted request bucket quota. Retry after cool-down window.");
            }

            if (!path.Contains("system/version", StringComparison.OrdinalIgnoreCase) && !path.Contains("jwks", StringComparison.OrdinalIgnoreCase) && path.StartsWith("health", StringComparison.OrdinalIgnoreCase))
            {
                AddErrorResponse("503", "Service Unavailable", "DEPENDENCY_UNAVAILABLE", "A critical backend storage or message queuing dependency is momentarily unreachable.");
            }

            return Task.CompletedTask;
        });

        return options;
    }

    public static OpenApiOptions AddEmcoreSwaggerVersioning(this OpenApiOptions options, string documentName = "v1", string apiVersion = "1.0.0")
    {
        options.AddOperationTransformer((operation, context, ct) =>
        {
            if (string.IsNullOrWhiteSpace(operation.OperationId))
            {
                var method = (context.Description.HttpMethod ?? "GET").ToLowerInvariant();
                var path = (context.Description.RelativePath ?? string.Empty).Split('?', '#')[0];
                var parts = path.Split(new[] { '/', '{', '}', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                var titleCaseParts = parts.Select(p => p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1) : string.Empty);
                operation.OperationId = $"{method}{string.Join("", titleCaseParts)}";
            }

            if (operation.Tags == null || operation.Tags.Count == 0)
            {
                operation.Tags = new List<OpenApiTag>();
                var path = context.Description.RelativePath ?? string.Empty;
                if (path.StartsWith("health", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "System Health", Description = "Liveness, readiness, and monitoring probe endpoints." });
                else if (path.Contains("system/version", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "System Metadata", Description = "System operational build and environment discovery endpoints." });
                else if (path.Contains("auth/mfa", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "MFA", Description = "Multi-factor authentication challenge and configuration workflows." });
                else if (path.Contains("auth/stepup", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "Step-Up Authentication", Description = "Step-up authorization workflows for privileged actions." });
                else if (path.Contains("auth/password", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "Password Recovery", Description = "Account credential recovery and self-service password modifications." });
                else if (path.Contains("auth/sessions", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "Sessions", Description = "Active user authentication session inspection and remote revocation." });
                else if (path.Contains("auth", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "Authentication", Description = "User onboarding, verification, credential validation, and JWT token issuance." });
                else if (path.Contains("service-clients", StringComparison.OrdinalIgnoreCase) || path.EndsWith("token", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "Service Clients", Description = "Workload identity administration, machine authentication, and secret rotation." });
                else if (path.Contains("admin", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "Administrative Security", Description = "Elevated administrative identity controls and user locking operations." });
                else if (path.Contains("identity", StringComparison.OrdinalIgnoreCase))
                    operation.Tags.Add(new OpenApiTag { Name = "User Profiles", Description = "Authenticated identity self-discovery and account status verification." });
                else
                    operation.Tags.Add(new OpenApiTag { Name = "Core Operations", Description = "General service domain endpoint operations." });
            }

            var relPath = context.Description.RelativePath ?? string.Empty;
            if (relPath.StartsWith("health/live", StringComparison.OrdinalIgnoreCase))
            {
                operation.Summary ??= "Instantaneous liveness probe check";
                operation.Description ??= "Returns HTTP 200 OK without evaluating downstream network or storage dependencies to confirm that the container process is actively running without deadlocks. Excludes rate limiting.";
            }
            else if (relPath.StartsWith("health/ready", StringComparison.OrdinalIgnoreCase))
            {
                operation.Summary ??= "System runtime dependency readiness probe";
                operation.Description ??= "Evaluates real-time connectivity to core runtime infrastructure (databases, caches, event brokers). Returns 200 OK when service is fully initialized and ready to safely handle consumer traffic.";
            }
            else if (relPath.Equals("health", StringComparison.OrdinalIgnoreCase) || relPath.Equals("healthz", StringComparison.OrdinalIgnoreCase))
            {
                operation.Summary ??= "General service health check indicator";
                operation.Description ??= "Performs general system diagnostic health assessment for load balancer routing decisions and automated monitoring alerts.";
            }
            else if (relPath.Contains("system/version", StringComparison.OrdinalIgnoreCase))
            {
                operation.Summary ??= "Retrieve service deployment release metadata";
                operation.Description ??= "Returns executable release metadata including service logical identifier, semantic version string, and runtime environment designation. Used for contract compatibility validation and diagnostic tracing.";
            }

            return Task.CompletedTask;
        });

        return options;
    }

    public static OpenApiOptions AddEmcoreSwaggerExamples(this OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, ct) =>
        {
            if (schema.Properties != null && schema.Properties.Count > 0)
            {
                foreach (var prop in schema.Properties)
                {
                    var name = prop.Key;
                    var propSchema = prop.Value;

                    if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "userId", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "targetUserId", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("UserId", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Opaque unique identity reference identifier (ULID/UUID format). Must not be treated as a sequential database key.";
                        propSchema.Example = new OpenApiString("usr_01HPX7K7R5YZ2X90WY0001");
                    }
                    else if (string.Equals(name, "organizationId", StringComparison.OrdinalIgnoreCase) || name.EndsWith("OrganizationId", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Opaque string identifier representing the requested organization context. Supplying this value does not grant authorization. Runtime membership validation is not currently verified as active. The authorization layer must validate requested context against the authenticated principal before it can be trusted.";
                        propSchema.Example = new OpenApiString("org_01HPX7K7R5YZ2X90WY0002");
                    }
                    else if (string.Equals(name, "listingId", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "itemId", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Opaque catalog listing item reference identifier.";
                        propSchema.Example = new OpenApiString("lst_01HPX7K7R5YZ2X90WY0003");
                    }
                    else if (string.Equals(name, "email", StringComparison.OrdinalIgnoreCase) || name.Contains("email", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Normalized electronic mail communication address.";
                        propSchema.Example = new OpenApiString("developer@emcore.platform");
                    }
                    else if (string.Equals(name, "mobile", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "phoneNumber", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "International E.164 formatted mobile phone contact number.";
                        propSchema.Example = new OpenApiString("+971500000000");
                    }
                    else if (string.Equals(name, "password", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "currentPassword", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "newPassword", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Sensitive authentication secret credential. Must satisfy complex entropy requirements. Never logged or retained in plaintext.";
                        propSchema.Example = new OpenApiString("********");
                    }
                    else if (string.Equals(name, "token", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "refreshToken", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "accessToken", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Cryptographically secure verification or session authorization token. Treated as sensitive credential.";
                        propSchema.Example = new OpenApiString("sample_sanitized_token_value");
                    }
                    else if (string.Equals(name, "currency", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "currencyCode", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Three-letter ISO 4217 standard currency code.";
                        propSchema.Example = new OpenApiString("AED");
                    }
                    else if (string.Equals(name, "amount", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "price", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Exact monetary value represented as a non-floating point decimal amount.";
                        propSchema.Example = new OpenApiDouble(250.50);
                    }
                    else if (string.Equals(name, "status", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Current operational or workflow lifecycle state indicator.";
                        propSchema.Example = new OpenApiString("Active");
                    }
                    else if (string.Equals(name, "createdAt", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "updatedAt", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "timestamp", StringComparison.OrdinalIgnoreCase))
                    {
                        propSchema.Description = "Coordinated Universal Time (UTC) timestamp formatted according to ISO 8601.";
                        propSchema.Example = new OpenApiString("2026-08-06T12:00:00Z");
                    }
                }
            }
            return Task.CompletedTask;
        });

        return options;
    }
}
