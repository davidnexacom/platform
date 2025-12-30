using FSH.Playground.Blazor.ApiClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FSH.Playground.Blazor.Services;

#pragma warning disable CA1515 // Extension method classes must be public
internal static class SimpleBffAuth
#pragma warning restore CA1515
{
    public static void MapSimpleBffAuthEndpoints(this WebApplication app)
    {
        // Login endpoint - calls identity API, sets cookie, returns success
        // Note: Uses /bff/ prefix to avoid conflict with ALB routing /api/* to the API service
        app.MapPost("/bff/auth/login", async (
            HttpContext httpContext,
            ITokenClient tokenClient,
            ILogger<Program> logger) =>
        {
            try
            {
                // Read form data
                var form = await httpContext.Request.ReadFormAsync();
                var email = form["Email"].ToString();
                var password = form["Password"].ToString();
                var tenant = form["Tenant"].ToString();

                logger.LogInformation("Login attempt for {Email}", email);

                // Call the identity API to get token
                var token = await tokenClient.IssueAsync(
                    tenant ?? "root",
                    new GenerateTokenCommand
                    {
                        Email = email,
                        Password = password
                    });

                if (token == null || string.IsNullOrEmpty(token.AccessToken))
                {
                    return Results.Unauthorized();
                }

                // Parse JWT to extract claims
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(token.AccessToken);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, jwtToken.Subject ?? Guid.NewGuid().ToString()),
                    new(ClaimTypes.Email, email),
                    new("access_token", token.AccessToken), // Store JWT for API calls
                    new("refresh_token", token.RefreshToken), // Store refresh token for token renewal
                    new("tenant", tenant ?? "root"), // Store tenant for token refresh
                };

                // Add name claim
                var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name);
                if (nameClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
                }

                // Add role claims
                var roleClaims = jwtToken.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role);
                claims.AddRange(roleClaims.Select(r => new Claim(ClaimTypes.Role, r.Value)));

                // Create identity and sign in with cookie
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                logger.LogInformation("Login successful for {Email}", email);

                // Redirect to home page - this ensures the cookie is properly read on the next request
                return Results.Redirect("/");
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login failed");
                return Results.Problem("Login failed");
            }
        })
        .AllowAnonymous()
        .DisableAntiforgery();

        // Logout endpoint - POST for API calls
        app.MapPost("/bff/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("Cookies");
            return Results.Ok();
        })
        .DisableAntiforgery();

        // Logout endpoint - GET for browser redirects (ensures cookie is cleared in browser)
        app.MapGet("/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("Cookies");
            return Results.Redirect("/login?toast=logout_success");
        })
        .AllowAnonymous();

        // Impersonate endpoint - updates cookie with impersonation token
        app.MapGet("/bff/auth/impersonate", async (
            HttpContext httpContext,
            ILogger<Program> logger) =>
        {
            try
            {
                var accessToken = httpContext.Request.Query["accessToken"].ToString();
                var refreshToken = httpContext.Request.Query["refreshToken"].ToString();

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    return Results.BadRequest("Missing tokens");
                }

                // Parse JWT to extract claims including impersonation claims
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(accessToken);

                var claims = new List<Claim>
                {
                    new("access_token", accessToken),
                    new("refresh_token", refreshToken),
                };

                // Extract all claims from the JWT
                foreach (var claim in jwtToken.Claims)
                {
                    // Add all claims except jti (unique token identifier)
                    if (claim.Type != "jti")
                    {
                        claims.Add(new Claim(claim.Type, claim.Value));
                    }
                }

                // Ensure we have the required claims
                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    var sub = jwtToken.Subject;
                    if (!string.IsNullOrEmpty(sub))
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
                    }
                }

                // Get tenant from claims or query
                var tenant = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant")?.Value 
                            ?? httpContext.Request.Query["tenant"].ToString() 
                            ?? "root";
                
                if (!claims.Any(c => c.Type == "tenant"))
                {
                    claims.Add(new Claim("tenant", tenant));
                }

                // Create new identity and sign in
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignOutAsync("Cookies");
                await httpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                logger.LogInformation("Impersonation session started");

                // Redirect to home with impersonation banner
                return Results.Redirect("/");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Impersonation failed");
                return Results.Problem("Impersonation failed");
            }
        })
        .RequireAuthorization();

        // Stop impersonation endpoint - calls backend and restores original user token
        app.MapGet("/bff/auth/stop-impersonation", async (
            HttpContext httpContext,
            IHttpClientFactory httpClientFactory,
            ILogger<Program> logger) =>
        {
            try
            {
                var user = httpContext.User;
                
                // Verify we're in impersonation mode
                var isImpersonating = user.FindFirst("impersonator") != null;
                if (!isImpersonating)
                {
                    logger.LogWarning("Stop impersonation called but not impersonating");
                    return Results.Redirect("/?toast=not_impersonating");
                }

                // Get current access token from claims
                var currentAccessToken = user.FindFirst("access_token")?.Value;
                if (string.IsNullOrEmpty(currentAccessToken))
                {
                    logger.LogError("No access token found in claims");
                    await httpContext.SignOutAsync("Cookies");
                    return Results.Redirect("/login?toast=session_expired");
                }

                // Call the backend API to stop impersonation and get original user token
                var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", currentAccessToken);
                
                var tenant = user.FindFirst("tenant")?.Value ?? "root";
                httpClient.DefaultRequestHeaders.Add("X-Tenant", tenant);
                
                var apiBaseUrl = httpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["Api:BaseUrl"] 
                    ?? throw new InvalidOperationException("Api:BaseUrl not configured");

                var response = await httpClient.PostAsync(
                    $"{apiBaseUrl}/api/v1/identity/impersonate/stop", 
                    null);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to stop impersonation: {StatusCode}", response.StatusCode);
                    await httpContext.SignOutAsync("Cookies");
                    return Results.Redirect("/login?toast=stop_failed");
                }

                var tokenJson = await response.Content.ReadAsStringAsync();
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(
                    tokenJson,
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    logger.LogError("Invalid token response from API");
                    await httpContext.SignOutAsync("Cookies");
                    return Results.Redirect("/login?toast=stop_failed");
                }

                // Parse JWT to extract claims of original user
                var jwtHandler = new JwtSecurityTokenHandler();
                var jwtToken = jwtHandler.ReadJwtToken(tokenResponse.AccessToken);

                var claims = new List<Claim>
                {
                    new("access_token", tokenResponse.AccessToken),
                    new("refresh_token", tokenResponse.RefreshToken),
                };

                // Extract all claims from the JWT
                foreach (var claim in jwtToken.Claims)
                {
                    if (claim.Type != "jti")
                    {
                        claims.Add(new Claim(claim.Type, claim.Value));
                    }
                }

                // Ensure required claims
                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    var sub = jwtToken.Subject;
                    if (!string.IsNullOrEmpty(sub))
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
                    }
                }

                if (!claims.Any(c => c.Type == "tenant"))
                {
                    claims.Add(new Claim("tenant", tenant));
                }

                // Create new identity for original user (without impersonation claims)
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignOutAsync("Cookies");
                await httpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                logger.LogInformation("Impersonation session stopped, restored original user");

                return Results.Redirect("/?toast=impersonation_stopped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stop impersonation failed");
                await httpContext.SignOutAsync("Cookies");
                return Results.Redirect("/login?toast=stop_failed");
            }
        })
        .RequireAuthorization();
    }
}
