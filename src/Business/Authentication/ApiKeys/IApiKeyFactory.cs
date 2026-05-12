namespace Business.Authentication.ApiKeys;

public interface IApiKeyFactory
{
    /// <summary>
    /// Generates a new key in the canonical <c>shf_&lt;8-prefix&gt;_&lt;32-secret&gt;</c> format.
    /// Returns the plaintext (shown to the user exactly once), prefix, last-4, and SHA-256 hash
    /// of the FULL token.
    /// </summary>
    GeneratedApiKey Generate();

    /// <summary>
    /// Parses a presented token. Returns false if the format is invalid.
    /// </summary>
    bool TryParse(string token, out string prefix, out string secret);

    /// <summary>
    /// SHA-256 hex of the full token (<c>shf_&lt;prefix&gt;_&lt;secret&gt;</c>).
    /// </summary>
    string Hash(string token);
}

public sealed record GeneratedApiKey(string Plaintext, string Prefix, string Last4, string KeyHash);
