namespace Business.Authentication.Sessions;

public interface IRefreshTokenFactory
{
    /// <summary>
    /// Generates a fresh plaintext token + its SHA-256 hash. Plaintext is returned to the client
    /// exactly once; only the hash is persisted.
    /// </summary>
    (string Plaintext, string Hash) Generate();

    /// <summary>
    /// Hashes a plaintext token presented by a client so it can be looked up in the store
    /// without revealing the token to anyone reading the DB.
    /// </summary>
    string Hash(string plaintext);
}
