using System.Collections.Concurrent;
using Domain.Entities.Identity;

namespace Business.Identity;

internal sealed class InMemoryVerificationStore : IVerificationStore
{
    private readonly ConcurrentDictionary<Guid, Verification> _verifications = new();

    public Task<Verification> AddAsync(Verification verification, CancellationToken ct = default)
    {
        _verifications[verification.Id] = verification;
        return Task.FromResult(verification);
    }

    public Task<Verification?> FindActiveAsync(Guid tenantId, Guid userId, VerificationChannel channel, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var match = _verifications.Values
            .Where(v => v.TenantId == tenantId && v.UserId == userId && v.Channel == channel)
            .Where(v => v.Status == VerificationStatus.Pending && v.ExpiresAt > now)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult<Verification?>(match);
    }

    public Task<Verification?> FindByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        _verifications.TryGetValue(id, out var verification);
        return Task.FromResult(verification is not null && verification.TenantId == tenantId ? verification : null);
    }

    public Task ConsumeAsync(Guid id, CancellationToken ct = default)
    {
        if (_verifications.TryGetValue(id, out var verification))
        {
            verification.ConsumedAt = DateTime.UtcNow;
            verification.Status = VerificationStatus.Consumed;
        }
        return Task.CompletedTask;
    }
}
