using System.Security.Claims;

namespace FSH.Framework.Core.Context;

public static class ImpersonationExtensions
{
    /// <summary>
    /// Checks if the current user is impersonating another user.
    /// </summary>
    public static bool IsImpersonating(this ICurrentUser currentUser)
    {
        var claims = currentUser.GetUserClaims();
        return claims?.Any(c => c.Type == ImpersonationClaims.Impersonator) ?? false;
    }

    /// <summary>
    /// Gets the impersonator's user ID if the current user is being impersonated.
    /// </summary>
    public static string? GetImpersonatorId(this ICurrentUser currentUser)
    {
        var claims = currentUser.GetUserClaims();
        return claims?.FirstOrDefault(c => c.Type == ImpersonationClaims.Impersonator)?.Value;
    }

    /// <summary>
    /// Gets the impersonator's name if the current user is being impersonated.
    /// </summary>
    public static string? GetImpersonatorName(this ICurrentUser currentUser)
    {
        var claims = currentUser.GetUserClaims();
        return claims?.FirstOrDefault(c => c.Type == ImpersonationClaims.ImpersonatorName)?.Value;
    }

    /// <summary>
    /// Gets the original user ID (impersonator) if impersonating.
    /// </summary>
    public static string? GetOriginalUserId(this ICurrentUser currentUser)
    {
        var claims = currentUser.GetUserClaims();
        return claims?.FirstOrDefault(c => c.Type == ImpersonationClaims.OriginalUserId)?.Value;
    }
}
