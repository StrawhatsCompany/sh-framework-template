namespace Business.Libraries.Authentication.Authorization;

public static class AuthorizationClaims
{
    /// <summary>Custom claim type for direct per-principal permissions (independent of roles).</summary>
    public const string Permission = "permissions";
}
