namespace Business.Authentication.Sso;

public sealed class SsoOptions
{
    public const string SectionName = "Authentication:Sso";

    /// <summary>
    /// When true, callbacks with an IdP-asserted email that has no matching local user
    /// auto-create the User row (Status=Active, EmailVerifiedAt=now). When false, callbacks
    /// without a matching user fail with <c>UserProvisioningRefused</c>.
    /// </summary>
    public bool AutoCreateUser { get; set; } = true;

    /// <summary>
    /// When true, auto-created users get <c>EmailVerifiedAt</c> set immediately — we trust
    /// the IdP's email verification. Disable for IdPs you don't fully trust.
    /// </summary>
    public bool AutoVerifyEmail { get; set; } = true;

    /// <summary>CSV of role names to assign to auto-created users. Typically empty.</summary>
    public string DefaultRoleNames { get; set; } = "";

    /// <summary>
    /// Allowed return-URL prefixes (one per CSV entry). The <c>start</c> endpoint validates
    /// the <c>returnUrl</c> against this list to prevent open-redirect attacks. Empty list
    /// rejects every returnUrl (callers must omit and accept the configured default).
    /// </summary>
    public string AllowedReturnUrls { get; set; } = "";

    /// <summary>Cookie name carrying the OAuth state + PKCE verifier + nonce during the handshake.</summary>
    public string StateCookieName { get; set; } = "sho_sso_state";

    public TimeSpan StateCookieTtl { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(60);
}
