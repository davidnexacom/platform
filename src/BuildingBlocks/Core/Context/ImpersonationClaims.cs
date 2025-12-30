namespace FSH.Framework.Core.Context;

/// <summary>
/// Claim type constants for impersonation functionality.
/// </summary>
public static class ImpersonationClaims
{
    /// <summary>
    /// Claim type for the user ID of the person doing the impersonation.
    /// </summary>
    public const string Impersonator = "impersonator";
    
    /// <summary>
    /// Claim type for the full name of the person doing the impersonation.
    /// </summary>
    public const string ImpersonatorName = "impersonator_name";
    
    /// <summary>
    /// Claim type for the original user ID (same as impersonator).
    /// </summary>
    public const string OriginalUserId = "original_user_id";
}
