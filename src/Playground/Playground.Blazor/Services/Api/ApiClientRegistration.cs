using System.Net.Http;
using FSH.Playground.Blazor.ApiClient;
using FSH.Playground.Blazor.Services.Api;

namespace FSH.Playground.Blazor;

internal static class ApiClientRegistration
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        var apiBaseUrl = configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing.");

        // Register handlers
        services.AddTransient<ForwardedHeadersHandler>();
        services.AddTransient<AuthorizationHeaderHandler>();

        // Register a named HttpClient for token operations (no auth header to avoid circular dependency)
        services.AddHttpClient("TokenClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .AddHttpMessageHandler<ForwardedHeadersHandler>();

        // Register the main authenticated HttpClient with both handlers
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .AddHttpMessageHandler<ForwardedHeadersHandler>()  // First: forward browser headers
        .AddHttpMessageHandler<AuthorizationHeaderHandler>(); // Second: add auth token

        // TokenClient uses the named HttpClient without the AuthorizationHeaderHandler
        // This avoids circular dependency: TokenRefreshService -> ITokenClient -> HttpClient -> AuthorizationHeaderHandler -> TokenRefreshService
        services.AddTransient<ITokenClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("TokenClient");
            return new TokenClient(client);
        });

        // All other clients use the authenticated HttpClient with ForwardedHeadersHandler
        services.AddTransient<IIdentityClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new IdentityClient(client);
        });

        services.AddTransient<IAuditsClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new AuditsClient(client);
        });

        services.AddTransient<ITenantsClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new TenantsClient(client);
        });

        services.AddTransient<IUsersClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new UsersClient(client);
        });

        services.AddTransient<IGroupsClient>(sp =>
            new GroupsClient(ResolveClient(sp)));

        services.AddTransient<ISessionsClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new SessionsClient(client);
        });

        services.AddTransient<IV1Client>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new V1Client(client);
        });

        services.AddTransient<IHealthClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            return new HealthClient(client);
        });

        services.AddTransient<IExternalGatewayClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ApiClient");
            var logger = sp.GetRequiredService<ILogger<ExternalGatewayClient>>();
            return new ExternalGatewayClient(client, logger);
        });

        return services;
    }
}
