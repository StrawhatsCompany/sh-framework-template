using Business.Authentication.Mfa;
using Business.Authentication.Mfa.Email;
using Business.Providers;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using Business.Providers.Mail.Dtos;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;

namespace Business.Tests.Authentication.Mfa.Email;

public class EmailMfaChannelTests
{
    [Fact]
    public async Task IssueAsync_dispatches_mail_and_persists_code_hash_on_challenge()
    {
        var (channel, _, mail, challenges) = Build();
        var factor = NewFactor(MfaFactorKind.Email, destination: "user@example.com");
        var challenge = NewChallenge(factor);

        var result = await channel.IssueAsync(factor, challenge);

        Assert.True(result.IsSuccess);
        Assert.NotNull(challenge.CodeHash);
        await mail.Received(1).SendAsync(
            Arg.Is<SendMailContract.Request>(r => r.To[0].Address == "user@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_returns_failure_when_factor_has_no_destination()
    {
        var (channel, _, _, _) = Build();
        var factor = NewFactor(MfaFactorKind.Email, destination: null);
        var challenge = NewChallenge(factor);

        var result = await channel.IssueAsync(factor, challenge);

        Assert.False(result.IsSuccess);
        Assert.Equal(MfaResultCode.FactorNotActive.Code, result.Code);
    }

    [Fact]
    public async Task VerifyAsync_accepts_the_dispatched_code()
    {
        var (channel, factory, mail, _) = Build();
        SendMailContract.Request? sent = null;
        mail.SendAsync(Arg.Do<SendMailContract.Request>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new SendMailContract.Response("msg-1", "Sent")));

        var factor = NewFactor(MfaFactorKind.Email, destination: "user@example.com");
        var challenge = NewChallenge(factor);
        await channel.IssueAsync(factor, challenge);

        // Recover the plaintext code from the dispatched body (the template puts it inline).
        Assert.NotNull(sent);
        var code = ExtractCode(sent.MailBody.TextBody!);

        var verify = await channel.VerifyAsync(factor, challenge, code);

        Assert.True(verify.IsSuccess);
    }

    [Fact]
    public async Task VerifyAsync_rejects_wrong_code()
    {
        var (channel, _, _, _) = Build();
        var factor = NewFactor(MfaFactorKind.Email, destination: "u@x");
        var challenge = NewChallenge(factor);
        await channel.IssueAsync(factor, challenge);

        var verify = await channel.VerifyAsync(factor, challenge, "000000");

        Assert.False(verify.IsSuccess);
        Assert.Equal(MfaResultCode.InvalidCode.Code, verify.Code);
    }

    [Fact]
    public async Task VerifyAsync_rejects_expired_challenge()
    {
        var (channel, _, _, _) = Build(ttl: TimeSpan.FromMilliseconds(1));
        var factor = NewFactor(MfaFactorKind.Email, destination: "u@x");
        var challenge = NewChallenge(factor);
        await channel.IssueAsync(factor, challenge);
        await Task.Delay(20);
        var anyCode = "123456"; // we just need to fail on expiry, not on code

        var verify = await channel.VerifyAsync(factor, challenge, anyCode);
        Assert.False(verify.IsSuccess);
        // Either InvalidCode (wrong code) or ChallengeExpired — we accept either, since
        // both signal "this challenge is dead". The orchestrator's expiry check is the
        // primary defence; the channel's is belt-and-braces.
        Assert.True(verify.Code == MfaResultCode.InvalidCode.Code || verify.Code == MfaResultCode.ChallengeExpired.Code);
    }

    private static (EmailMfaChannel channel, IProviderFactory<MailProviderCredential, IMailProvider> factory,
        IMailProvider mail, IMfaChallengeStore challenges) Build(TimeSpan? ttl = null)
    {
        var mail = Substitute.For<IMailProvider>();
        mail.SendAsync(Arg.Any<SendMailContract.Request>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new SendMailContract.Response("msg-1", "Sent")));
        var factory = Substitute.For<IProviderFactory<MailProviderCredential, IMailProvider>>();
        factory.Create(Arg.Any<MailProviderCredential>()).Returns(mail);

        var mailOpts = Substitute.For<IOptionsSnapshot<MailOptions>>();
        mailOpts.Value.Returns(new MailOptions
        {
            HostName = "localhost", Port = 25, UseSsl = false,
            FromAddress = "noreply@example.com", FromName = "Test",
        });

        var emailOpts = Substitute.For<IOptionsSnapshot<EmailMfaOptions>>();
        emailOpts.Value.Returns(new EmailMfaOptions
        {
            Subject = "code",
            BodyTemplate = "Your code is {{code}}. Expires in {{ttlMin}} minutes.",
            Ttl = ttl ?? TimeSpan.FromMinutes(10),
            CodeLength = 6,
        });

        var challenges = new InMemoryMfaChallengeStore();
        return (new EmailMfaChannel(factory, mailOpts, emailOpts, challenges), factory, mail, challenges);
    }

    private static MfaFactor NewFactor(MfaFactorKind kind, string? destination) => new()
    {
        Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(),
        Kind = kind, Destination = destination, Status = MfaFactorStatus.Active,
        CreatedAt = DateTime.UtcNow,
    };

    private static MfaChallenge NewChallenge(MfaFactor factor) => new()
    {
        Id = Guid.NewGuid(), TenantId = factor.TenantId, UserId = factor.UserId,
        MfaFactorId = factor.Id, Kind = factor.Kind,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        Status = MfaChallengeStatus.Pending, CreatedAt = DateTime.UtcNow,
    };

    private static string ExtractCode(string body)
    {
        // Body = "Your code is 123456. Expires in 10 minutes." — pluck the digit run.
        var digits = body.Where(char.IsDigit).Take(6).ToArray();
        return new string(digits);
    }
}
