namespace FSH.Playground.Blazor.Services;

/// <summary>
/// Service that notifies Blazor components when authentication state changes
/// (e.g., when token refresh fails and user needs to re-login).
/// This is necessary because HTTP redirects don't work in Blazor Server's SignalR context.
/// </summary>
internal interface IAuthStateNotifier
{
    /// <summary>
    /// Event fired when the user's session has expired and they need to re-authenticate.
    /// </summary>
    event EventHandler? SessionExpired;

    /// <summary>
    /// Notify subscribers that the session has expired.
    /// </summary>
    void NotifySessionExpired();
}

internal sealed class AuthStateNotifier : IAuthStateNotifier
{
    public event EventHandler? SessionExpired;

    public void NotifySessionExpired()
    {
        SessionExpired?.Invoke(this, EventArgs.Empty);
    }
}
