namespace Business.Libraries.Authentication.Authorization;

/// <summary>
/// Role-to-permission mapping bound from <c>Authorization:Roles</c>. Each key is a role name;
/// each value is the set of permissions members of that role hold.
/// </summary>
public sealed class AuthorizationModelOptions
{
    public const string SectionName = "Authorization";

    public Dictionary<string, string[]> Roles { get; init; } = new(StringComparer.Ordinal);
}
