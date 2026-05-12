using Domain.Entities.Identity;

namespace Business.Authentication.Sessions;

public interface IRefreshTokenStore
{
    Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<RefreshToken?> UpdateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Walks the rotation chain in both directions (via ReplacedById / parent links) and revokes
    /// every member. Used on detected reuse — the entire token family is burned.
    /// </summary>
    Task RevokeFamilyAsync(Guid refreshTokenId, CancellationToken ct = default);

    /// <summary>
    /// Revokes every refresh token attached to the session, regardless of chain position.
    /// </summary>
    Task RevokeAllForSessionAsync(Guid sessionId, CancellationToken ct = default);
}
