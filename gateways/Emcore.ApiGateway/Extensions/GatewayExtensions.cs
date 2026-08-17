using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Emcore.ApiGateway.Security;
using Emcore.ServiceDefaults;
using Yarp.ReverseProxy.Transforms;

namespace Emcore.ApiGateway.Extensions;

public static class GatewayExtensions
{
    public static IHostApplicationBuilder AddGatewayServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var isProduction = builder.Environment.IsProduction();

        // Register ServiceDefaults and OpenTelemetry
        builder.AddServiceDefaults();

        // 1. Forwarded Headers Configuration with explicit proxy trust verification
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = configuration.GetValue<int>("Gateway:ForwardLimit", 1);

            // Clear defaults before explicitly adding trusted proxies and networks
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            var trustedProxies = configuration.GetSection("Gateway:TrustedProxies").Get<string[]>() ?? new[] { "127.0.0.1", "::1" };
            foreach (var proxy in trustedProxies)
            {
                if (IPAddress.TryParse(proxy, out var ip))
                {
                    options.KnownProxies.Add(ip);
                }
            }

            var trustedNetworks = configuration.GetSection("Gateway:TrustedNetworks").Get<string[]>() ?? Array.Empty<string>();
            foreach (var net in trustedNetworks)
            {
                var parts = net.Split('/');
                if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var prefixLength))
                {
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
                }
                else if (IPAddress.TryParse(net, out var ip))
                {
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(ip, 32));
                }
            }
        });

        // 2. Request Body Limits and Kestrel timeouts
        var maxRequestBodySize = configuration.GetValue<long?>("Gateway:MaxRequestBodySizeBytes", 10485760);
        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = maxRequestBodySize;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Gateway:RequestTimeoutSeconds", 30));
        });
        builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxRequestBodySize ?? 10485760;
        });

        // 3. CORS Policy Registration with strict Production validation
        var allowedOrigins = configuration.GetSection("Gateway:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        allowedOrigins = allowedOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();

        if (isProduction && allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Missing required Production configuration: 'Gateway:AllowedOrigins' must contain at least one valid CORS origin when running in Production. Configure via environment variables (e.g., Gateway__AllowedOrigins__0=https://confirmed-public-domain). No AllowAnyOrigin fallback is permitted.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("GatewayCorsPolicy", corsBuilder =>
            {
                if (allowedOrigins.Length > 0)
                {
                    corsBuilder.WithOrigins(allowedOrigins)
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .WithExposedHeaders("X-Request-Id", "X-Correlation-Id", "Retry-After");
                }
                else
                {
                    // Safe local developer fallback only in non-Production environments
                    corsBuilder.WithOrigins("http://localhost:5173", "http://localhost:3000")
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .WithExposedHeaders("X-Request-Id", "X-Correlation-Id", "Retry-After");
                }
            });
        });

        // 4. Rate Limiting Registration
        builder.Services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
                }
                else
                {
                    context.HttpContext.Response.Headers["Retry-After"] = "60";
                }
                await Task.CompletedTask;
            };

            // Anonymous policy: 60/minute partitioned by actual Remote IP
            var anonymousLimit = configuration.GetValue<int>("RateLimiting:Anonymous:PermitLimit", 60);
            options.AddPolicy("AnonymousPolicy", context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
                return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = anonymousLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Authenticated policy: 300/minute partitioned by User/Client ID, never raw Authorization header
            var authLimit = configuration.GetValue<int>("RateLimiting:Authenticated:PermitLimit", 300);
            options.AddPolicy("AuthenticatedPolicy", context =>
            {
                var partitionKey = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? context.User?.FindFirst("client_id")?.Value
                                   ?? context.Connection.RemoteIpAddress?.ToString()
                                   ?? "unknown-user";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Login/OTP policy: 10/minute partitioned by IP + endpoint path
            var loginLimit = configuration.GetValue<int>("RateLimiting:LoginOtp:PermitLimit", 10);
            options.AddPolicy("LoginOtpPolicy", context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
                var endpoint = context.Request.Path.ToString();
                return RateLimitPartition.GetFixedWindowLimiter($"{remoteIp}:{endpoint}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Health policy: exempt from normal user rate limiting
            options.AddPolicy("HealthPolicy", context =>
            {
                return RateLimitPartition.GetNoLimiter("health-exempt");
            });
        });

        // 5. Authentication and Authorization
        if (isProduction)
        {
            var issuer = configuration["Authentication:Issuer"];
            var audience = configuration["Authentication:Audience"];
            var signingKey = configuration["Authentication:SigningKey"];

            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(signingKey))
            {
                throw new InvalidOperationException("Missing required Production authentication configuration: 'Authentication:Issuer', 'Authentication:Audience', and 'Authentication:SigningKey' must be explicitly configured in Production environment via secure environment variables or vaults. Development authentication handlers cannot be used in Production.");
            }

            // When Identity Access token issuance is implemented, this placeholder will be upgraded to full JWT Bearer validation.
            // Until then, prevent fallback to TestAuthHandler in Production.
            throw new InvalidOperationException("Production JWT verification requires Identity Access token service implementation and cannot fall back to test authentication.");
        }
        else
        {
            // In local development or automated tests, use TestAuthHandler only inside tests without weakening Production
            builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
                   .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        }

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("PublicPolicy", policy => policy.RequireAssertion(_ => true));
            options.AddPolicy("AuthenticatedRoutePolicy", policy => policy.RequireAuthenticatedUser());
        });

        // 6. Register YARP Reverse Proxy
        builder.Services.AddReverseProxy()
               .LoadFromConfig(configuration.GetSection("ReverseProxy"))
               .AddTransforms(builderContext =>
               {
                   builderContext.AddRequestTransform(transformContext =>
                   {
                       var user = transformContext.HttpContext.User;
                       if (user?.Identity?.IsAuthenticated == true)
                       {
                           var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                        ?? user.FindFirst("sub")?.Value;

                           if (!string.IsNullOrWhiteSpace(userId))
                           {
                               transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                               transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-User-Id", userId);
                           }

                           transformContext.ProxyRequest.Headers.Remove("X-Session-Id");
                           var sessionId = user.FindFirst("sid")?.Value;
                           if (!string.IsNullOrWhiteSpace(sessionId))
                           {
                               transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-Session-Id", sessionId);
                           }
                       }
                       return ValueTask.CompletedTask;
                   });
               });

        return builder;
    }
}
