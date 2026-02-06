namespace FSH.Playground.Blazor.Services.Api;

/// <summary>
/// Circuit-scoped cache for storing the current access token.
///
/// In Blazor Server, httpContext.User claims are cached per circuit and don't update
/// even after SignInAsync. This service provides a way to store refreshed tokens
/// that can be used by subsequent requests within the same circuit.
///
/// Registered as Scoped, so each Blazor circuit gets its own instance.
/// </summary>
internal interface ICircuitTokenCache
{
    /// <summary>
    /// Gets the cached access token, or null if not set.
    /// </summary>
    string? AccessToken { get; }

    /// <summary>
    /// Gets the cached refresh token, or null if not set.
    /// </summary>
    string? RefreshToken { get; }

    /// <summary>
    /// Updates the cached tokens after a successful refresh.
    /// </summary>
    void UpdateTokens(string accessToken, string refreshToken);

    /// <summary>
    /// Clears the cached tokens (e.g., on logout or session expiration).
    /// </summary>
    void Clear();
}

internal sealed class CircuitTokenCache : ICircuitTokenCache
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }

    public void UpdateTokens(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
    }
}
