using FSH.Framework.Core.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace FSH.Modules.Identity.Authorization.Jwt;

public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _options;

    public ConfigureJwtBearerOptions(IOptions<JwtOptions> options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        _options = options.Value;
    }

    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Configure(string.Empty, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        byte[] key = Encoding.ASCII.GetBytes(_options.SigningKey);

        options.RequireHttpsMetadata = true;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidIssuer = _options.Issuer,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidAudience = _options.Audience,
            ValidateAudience = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();

                var path = context.HttpContext.Request.Path;
                
                // Static files and assets don't have endpoints - they're handled by UseStaticFiles()
                // If there's no endpoint, check if it's likely a static file request
                var endpoint = context.HttpContext.GetEndpoint();
                if (endpoint is null)
                {
                    // Allow requests without endpoints (static files, assets, etc.)
                    // These should have been handled by UseStaticFiles() middleware
                    // If they reach here, they're either missing files or the path doesn't exist
                    return Task.CompletedTask;
                }

                var allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

                // Only throw exception if authentication is required
                if (!allowAnonymous)
                {
                    var method = context.HttpContext.Request.Method;
                    throw new UnauthorizedException($"Unauthorized access to {method} {path}");
                }


                return Task.CompletedTask;
            },
            OnForbidden = _ => throw new ForbiddenException(),
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/notifications", StringComparison.OrdinalIgnoreCase))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    }
}
