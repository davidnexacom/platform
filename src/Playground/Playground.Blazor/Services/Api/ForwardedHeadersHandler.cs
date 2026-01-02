namespace FSH.Playground.Blazor.Services.Api;

/// <summary>
/// Delegating handler that forwards User-Agent and IP address from the browser/client
/// to the backend API when making requests from the BFF.
/// </summary>
internal sealed class ForwardedHeadersHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ForwardedHeadersHandler> _logger;

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
            // Forward User-Agent from the browser
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.Headers.TryAddWithoutValidation("X-Forwarded-User-Agent", userAgent);
            }

            // Forward IP address from the client connection
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // Use standard X-Forwarded-For header
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", ipAddress);
            }

            _logger.LogDebug(
                "Forwarding headers to API: User-Agent={UserAgent}, IP={IpAddress}",
                userAgent,
                ipAddress);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
