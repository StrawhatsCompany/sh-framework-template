using Business.Authentication.Mfa;
using Business.Authentication.Mfa.Sms;
using Business.Providers;
using Business.Providers.Sms;
using Business.Providers.Sms.Contracts;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;

namespace Business.Tests.Authentication.Mfa.Sms;

public class SmsMfaChannelTests
{
    [Fact]
    public async Task IssueAsync_dispatches_sms_and_persists_code_hash()
    {
        var (channel, _, sms, _) = Build();
        var factor = NewFactor(destination: "+15555550100");
        var challenge = NewChallenge(factor);

        var result = await channel.IssueAsync(factor, challenge);

        Assert.True(result.IsSuccess);
        Assert.NotNull(challenge.CodeHash);
        await sms.Received(1).SendAsync(
            Arg.Is<SendSmsContract.Request>(r => r.ToNumber == "+15555550100"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_returns_RateLimited_after_max_per_window()
    {
        // Use a fresh user id so this test isn't polluted by other tests via the static
        // process-wide rate-limit map.
        var userId = Guid.NewGuid();
        var (channel, _, _, _) = Build(rateMax: 2, rateWindow: TimeSpan.FromMinutes(10));

        for (var i = 0; i < 2; i++)
        {
            var factor = NewFactor(destination: "+1", userId: userId);
            var challenge = NewChallenge(factor);
            var ok = await channel.IssueAsync(factor, challenge);
            Assert.True(ok.IsSuccess);
        }

        var thirdFactor = NewFactor(destination: "+1", userId: userId);
        var thirdChallenge = NewChallenge(thirdFactor);
        var third = await channel.IssueAsync(thirdFactor, thirdChallenge);

        Assert.False(third.IsSuccess);
        Assert.Equal(MfaResultCode.RateLimited.Code, third.Code);
    }

    [Fact]
    public async Task IssueAsync_returns_DispatchFailed_when_provider_fails()
    {
        var (channel, factory, sms, _) = Build();
        sms.SendAsync(Arg.Any<SendSmsContract.Request>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Failure<SendSmsContract.Response>());
        var factor = NewFactor(destination: "+1");
        var challenge = NewChallenge(factor);

        var result = await channel.IssueAsync(factor, challenge);

        Assert.False(result.IsSuccess);
        Assert.Equal(MfaResultCode.DispatchFailed.Code, result.Code);
    }

    [Fact]
    public async Task VerifyAsync_accepts_dispatched_code()
    {
        var (channel, _, sms, _) = Build();
        SendSmsContract.Request? sent = null;
        sms.SendAsync(Arg.Do<SendSmsContract.Request>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new SendSmsContract.Response("sid-1", "queued")));
        var factor = NewFactor(destination: "+1");
        var challenge = NewChallenge(factor);
        await channel.IssueAsync(factor, challenge);

        Assert.NotNull(sent);
        var code = ExtractCode(sent.Body);

        var verify = await channel.VerifyAsync(factor, challenge, code);

        Assert.True(verify.IsSuccess);
    }

    [Fact]
    public async Task VerifyAsync_rejects_wrong_code()
    {
        var (channel, _, _, _) = Build();
        var factor = NewFactor(destination: "+1");
        var challenge = NewChallenge(factor);
        await channel.IssueAsync(factor, challenge);

        var verify = await channel.VerifyAsync(factor, challenge, "000000");

        Assert.False(verify.IsSuccess);
        Assert.Equal(MfaResultCode.InvalidCode.Code, verify.Code);
    }

    private static (SmsMfaChannel channel, IProviderFactory<SmsProviderCredential, ISmsProvider> factory,
        ISmsProvider sms, IMfaChallengeStore challenges) Build(int rateMax = 5, TimeSpan? rateWindow = null)
    {
        var sms = Substitute.For<ISmsProvider>();
        sms.SendAsync(Arg.Any<SendSmsContract.Request>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new SendSmsContract.Response("sid-1", "queued")));
        var factory = Substitute.For<IProviderFactory<SmsProviderCredential, ISmsProvider>>();
        factory.Create(Arg.Any<SmsProviderCredential>()).Returns(sms);

        var smsOpts = Substitute.For<IOptionsSnapshot<SmsOptions>>();
        smsOpts.Value.Returns(new SmsOptions
        {
            FromNumber = "+15555550000",
            AccountSid = "AC_test", AuthToken = "auth_test",
        });

        var mfaOpts = Substitute.For<IOptionsSnapshot<SmsMfaOptions>>();
        mfaOpts.Value.Returns(new SmsMfaOptions
        {
            BodyTemplate = "Your code is {{code}}",
            Ttl = TimeSpan.FromMinutes(5),
            CodeLength = 6,
            RateLimitMaxIssuesPerUser = rateMax,
            RateLimitWindow = rateWindow ?? TimeSpan.FromHours(1),
        });

        var challenges = new InMemoryMfaChallengeStore();
        return (new SmsMfaChannel(factory, smsOpts, mfaOpts, challenges), factory, sms, challenges);
    }

    private static MfaFactor NewFactor(string destination, Guid? userId = null) => new()
    {
        Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), UserId = userId ?? Guid.NewGuid(),
        Kind = MfaFactorKind.Sms, Destination = destination, Status = MfaFactorStatus.Active,
        CreatedAt = DateTime.UtcNow,
    };

    private static MfaChallenge NewChallenge(MfaFactor factor) => new()
    {
        Id = Guid.NewGuid(), TenantId = factor.TenantId, UserId = factor.UserId,
        MfaFactorId = factor.Id, Kind = factor.Kind,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        Status = MfaChallengeStatus.Pending, CreatedAt = DateTime.UtcNow,
    };

    private static string ExtractCode(string body) =>
        new(body.Where(char.IsDigit).Take(6).ToArray());
}
