using Microsoft.AspNetCore.Authentication;

namespace Business.Libraries.Authentication.ApiKey;

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>Header the handler reads the API key from. Defaults to <c>X-Api-Key</c>.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";
}
