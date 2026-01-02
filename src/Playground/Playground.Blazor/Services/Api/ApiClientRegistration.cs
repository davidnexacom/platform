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

        static HttpClient ResolveClient(IServiceProvider sp) =>
            sp.GetRequiredService<HttpClient>();

        // Register ForwardedHeadersHandler
        services.AddTransient<ForwardedHeadersHandler>();

        // Register a named HttpClient for token operations with ForwardedHeadersHandler
        services.AddHttpClient("TokenClient", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        })
        .AddHttpMessageHandler<ForwardedHeadersHandler>();

        // TokenClient uses the named HttpClient without the AuthorizationHeaderHandler
        // This avoids circular dependency: TokenRefreshService -> ITokenClient -> HttpClient -> AuthorizationHeaderHandler -> TokenRefreshService
        services.AddTransient<ITokenClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("TokenClient");
            return new TokenClient(client);
        });

        services.AddTransient<IIdentityClient>(sp =>
            new IdentityClient(ResolveClient(sp)));

        services.AddTransient<IAuditsClient>(sp =>
            new AuditsClient(ResolveClient(sp)));

        services.AddTransient<ITenantsClient>(sp =>
            new TenantsClient(ResolveClient(sp)));

        services.AddTransient<IUsersClient>(sp =>
            new UsersClient(ResolveClient(sp)));

        services.AddTransient<ISessionsClient>(sp =>
            new SessionsClient(ResolveClient(sp)));

        services.AddTransient<IV1Client>(sp =>
            new V1Client(ResolveClient(sp)));

        services.AddTransient<IHealthClient>(sp =>
            new HealthClient(ResolveClient(sp)));

        return services;
    }
}
