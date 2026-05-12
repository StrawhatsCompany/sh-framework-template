using Business.Authentication.Mfa;
using Business.Authentication.Mfa.Totp;
using Business.Configuration;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using OtpNet;

namespace Business.Tests.Authentication.Mfa;

public class MfaOrchestratorTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task IssueAsync_creates_pending_challenge_for_active_totp_factor()
    {
        var ctx = await BuildContextAsync(MfaFactorStatus.Active);

        var result = await ctx.orchestrator.IssueAsync(_tenantId, _userId, ctx.factor.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(MfaChallengeStatus.Pending, result.Data!.Status);
        Assert.Equal(ctx.factor.Id, result.Data.MfaFactorId);
    }

    [Fact]
    public async Task IssueAsync_refuses_disabled_factor()
    {
        var ctx = await BuildContextAsync(MfaFactorStatus.Disabled);

        var result = await ctx.orchestrator.IssueAsync(_tenantId, _userId, ctx.factor.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(MfaResultCode.FactorNotActive.Code, result.Code);
    }

    [Fact]
    public async Task VerifyAsync_succeeds_for_correct_code_and_marks_consumed()
    {
        var ctx = await BuildContextAsync(MfaFactorStatus.Active);
        var issue = await ctx.orchestrator.IssueAsync(_tenantId, _userId, ctx.factor.Id);
        Assert.True(issue.IsSuccess);

        var code = new Totp(Base32Encoding.ToBytes(ctx.secret), step: 30, mode: OtpHashMode.Sha1, totpSize: 6)
            .ComputeTotp(DateTime.UtcNow);

        var verify = await ctx.orchestrator.VerifyAsync(_tenantId, issue.Data!.Id, code);

        Assert.True(verify.IsSuccess);
        var reloaded = await ctx.challenges.FindByIdAsync(_tenantId, issue.Data.Id);
        Assert.Equal(MfaChallengeStatus.Consumed, reloaded!.Status);
        Assert.NotNull(reloaded.ConsumedAt);
    }

    [Fact]
    public async Task VerifyAsync_increments_FailedAttempts_on_wrong_code_and_fails_after_max()
    {
        var ctx = await BuildContextAsync(MfaFactorStatus.Active, maxFailedAttempts: 3);
        var issue = await ctx.orchestrator.IssueAsync(_tenantId, _userId, ctx.factor.Id);

        for (var i = 0; i < 3; i++)
        {
            await ctx.orchestrator.VerifyAsync(_tenantId, issue.Data!.Id, "000000");
        }

        var reloaded = await ctx.challenges.FindByIdAsync(_tenantId, issue.Data!.Id);
        Assert.Equal(MfaChallengeStatus.Failed, reloaded!.Status);

        // Next attempt fails with ChallengeFailed (not InvalidCode) because the challenge itself is dead.
        var next = await ctx.orchestrator.VerifyAsync(_tenantId, issue.Data.Id, "000000");
        Assert.False(next.IsSuccess);
        Assert.Equal(MfaResultCode.ChallengeFailed.Code, next.Code);
    }

    [Fact]
    public async Task VerifyAsync_rejects_expired_challenge()
    {
        var ctx = await BuildContextAsync(MfaFactorStatus.Active, challengeLifetime: TimeSpan.FromMilliseconds(1));
        var issue = await ctx.orchestrator.IssueAsync(_tenantId, _userId, ctx.factor.Id);
        Assert.True(issue.IsSuccess);

        await Task.Delay(50);
        var verify = await ctx.orchestrator.VerifyAsync(_tenantId, issue.Data!.Id, "000000");

        Assert.False(verify.IsSuccess);
        Assert.Equal(MfaResultCode.ChallengeExpired.Code, verify.Code);
    }

    private async Task<(IMfaOrchestrator orchestrator, IMfaFactorStore factors,
        IMfaChallengeStore challenges, MfaFactor factor, string secret)> BuildContextAsync(
            MfaFactorStatus status,
            int maxFailedAttempts = 5,
            TimeSpan? challengeLifetime = null)
    {
        var protector = new PlainProtector();
        var totp = new TotpService();
        var secret = totp.GenerateSecret();

        var factorStore = new InMemoryMfaFactorStore();
        var factor = new MfaFactor
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, UserId = _userId, Kind = MfaFactorKind.Totp,
            SecretCipher = protector.Protect(secret), Status = status, CreatedAt = DateTime.UtcNow,
        };
        await factorStore.AddAsync(factor);

        var challengeStore = new InMemoryMfaChallengeStore();
        var channel = new TotpMfaChannel(totp, protector);
        var options = Substitute.For<IOptionsSnapshot<MfaOptions>>();
        options.Value.Returns(new MfaOptions
        {
            MaxFailedAttempts = maxFailedAttempts,
            ChallengeLifetime = challengeLifetime ?? TimeSpan.FromMinutes(5),
        });

        var orchestrator = new MfaOrchestrator(factorStore, challengeStore, [channel], options);
        return (orchestrator, factorStore, challengeStore, factor, secret);
    }

    /// <summary>Pass-through protector for tests — no real encryption.</summary>
    private sealed class PlainProtector : ICredentialProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string ciphertext) => ciphertext;
    }
}
