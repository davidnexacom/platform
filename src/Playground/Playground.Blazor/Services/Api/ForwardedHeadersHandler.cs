namespace FSH.Playground.Blazor.Services.Api;

/// <summary>
/// Delegating handler that forwards User-Agent, Client Hints, and IP address from the browser/client
/// to the backend API when making requests from the BFF.
/// Client Hints provide more accurate device information than User-Agent parsing.
/// </summary>
internal sealed class ForwardedHeadersHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ForwardedHeadersHandler> _logger;

    // Client Hints headers we want to forward for device detection
    private static readonly string[] ClientHintsHeaders =
    [
        "Sec-CH-UA",
        "Sec-CH-UA-Mobile",
        "Sec-CH-UA-Platform",
        "Sec-CH-UA-Platform-Version",
        "Sec-CH-UA-Arch",
        "Sec-CH-UA-Model",
        "Sec-CH-UA-Full-Version-List"
    ];

    public ForwardedHeadersHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ForwardedHeadersHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            // Forward User-Agent from the browser (fallback for legacy support)
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.Headers.TryAddWithoutValidation("X-Forwarded-User-Agent", userAgent);
            }

            // Forward Client Hints headers for modern browser detection
            foreach (var header in ClientHintsHeaders)
            {
                if (httpContext.Request.Headers.TryGetValue(header, out var values))
                {
                    var value = values.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Forward with X-Forwarded- prefix to distinguish from direct API calls
                        request.Headers.TryAddWithoutValidation($"X-Forwarded-{header}", value);
                    }
                }
            }

            // Forward IP address from the client connection
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // Use standard X-Forwarded-For header
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", ipAddress);
            }

            _logger.LogDebug(
                "Forwarding headers to API: User-Agent={UserAgent}, IP={IpAddress}, Client-Hints={HasClientHints}",
                userAgent,
                ipAddress,
                httpContext.Request.Headers.ContainsKey("Sec-CH-UA"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
