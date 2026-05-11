using Business.Libraries.Authentication.Mfa;
using Microsoft.Extensions.Options;

namespace Business.Tests.Libraries.Authentication.Mfa;

public class MfaChallengeIssuerTests
{
    [Fact]
    public async Task IssueAsync_generates_code_of_configured_length_and_calls_channel_SendAsync()
    {
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel, codeLength: 6);

        var challenge = await issuer.IssueAsync("user-1", "Sms");

        Assert.NotNull(channel.SentCode);
        Assert.Equal(6, channel.SentCode!.Length);
        Assert.All(channel.SentCode, c => Assert.InRange(c, '0', '9'));
        Assert.Equal("user-1", challenge.UserId);
        Assert.Equal("Sms", challenge.ChannelType);
    }

    [Fact]
    public async Task IssueAsync_persists_challenge_with_hashed_code_only()
    {
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel);

        var challenge = await issuer.IssueAsync("user-1", "Sms");

        var saved = await store.GetAsync(challenge.ChallengeId);
        Assert.NotNull(saved);
        Assert.NotEqual(channel.SentCode, saved!.CodeHash); // hash, not plaintext
        Assert.Equal(64, saved.CodeHash.Length); // SHA-256 hex
    }

    [Fact]
    public async Task VerifyAsync_returns_Success_for_the_right_code()
    {
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel);
        var challenge = await issuer.IssueAsync("user-1", "Sms");

        var result = await issuer.VerifyAsync(challenge.ChallengeId, channel.SentCode!);

        Assert.Equal(MfaResult.Success, result);
    }

    [Fact]
    public async Task VerifyAsync_returns_Success_removes_challenge_from_store()
    {
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel);
        var challenge = await issuer.IssueAsync("user-1", "Sms");

        await issuer.VerifyAsync(challenge.ChallengeId, channel.SentCode!);

        Assert.Null(await store.GetAsync(challenge.ChallengeId));
    }

    [Fact]
    public async Task VerifyAsync_returns_Invalid_and_increments_attempts_on_wrong_code()
    {
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel);
        var challenge = await issuer.IssueAsync("user-1", "Sms");

        var result = await issuer.VerifyAsync(challenge.ChallengeId, "wrong0");

        Assert.Equal(MfaResult.Invalid, result);
        var stillThere = await store.GetAsync(challenge.ChallengeId);
        Assert.NotNull(stillThere);
        Assert.Equal(1, stillThere!.Attempts);
    }

    [Fact]
    public async Task VerifyAsync_returns_NoSuchChallenge_for_unknown_id()
    {
        var issuer = NewIssuer(new InMemoryStore(), new RecordingChannel("Sms"));

        var result = await issuer.VerifyAsync("does-not-exist", "123456");

        Assert.Equal(MfaResult.NoSuchChallenge, result);
    }

    [Fact]
    public async Task VerifyAsync_returns_Expired_when_past_the_window()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel, time: time);
        var challenge = await issuer.IssueAsync("user-1", "Sms");

        time.Advance(TimeSpan.FromMinutes(10));
        var result = await issuer.VerifyAsync(challenge.ChallengeId, channel.SentCode!);

        Assert.Equal(MfaResult.Expired, result);
        Assert.Null(await store.GetAsync(challenge.ChallengeId));
    }

    [Fact]
    public async Task VerifyAsync_returns_TooManyAttempts_after_exhausting_budget()
    {
        var channel = new RecordingChannel("Sms");
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel, maxAttempts: 3);
        var challenge = await issuer.IssueAsync("user-1", "Sms");

        await issuer.VerifyAsync(challenge.ChallengeId, "bad1");
        await issuer.VerifyAsync(challenge.ChallengeId, "bad2");
        await issuer.VerifyAsync(challenge.ChallengeId, "bad3");
        var fourth = await issuer.VerifyAsync(challenge.ChallengeId, "bad4");

        Assert.Equal(MfaResult.TooManyAttempts, fourth);
    }

    [Fact]
    public async Task IssueAsync_throws_when_no_channel_matches()
    {
        var issuer = NewIssuer(new InMemoryStore(), new RecordingChannel("Sms"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => issuer.IssueAsync("user-1", "Totp"));
    }

    [Fact]
    public async Task VerifyAsync_delegates_to_channel_VerifyAsync_for_TOTP_style_checks()
    {
        // A TOTP channel reports Valid from its VerifyAsync independently of the hash —
        // the orchestrator still gates on hash + channel both saying valid.
        var channel = new TotpStyleChannel();
        var store = new InMemoryStore();
        var issuer = NewIssuer(store, channel);
        var challenge = await issuer.IssueAsync("user-1", "Totp");

        var rightCode = await issuer.VerifyAsync(challenge.ChallengeId, channel.SentCode!);
        Assert.Equal(MfaResult.Success, rightCode);
    }

    private static MfaChallengeIssuer NewIssuer(
        IMfaCodeStore store,
        IMfaChannel channel,
        int codeLength = 6,
        int maxAttempts = 5,
        TimeProvider? time = null)
    {
        var options = Options.Create(new MfaOptions
        {
            CodeLength = codeLength,
            Expiry = TimeSpan.FromMinutes(5),
            MaxAttempts = maxAttempts,
        });
        return new MfaChallengeIssuer(store, [channel], options, time ?? TimeProvider.System);
    }

    private sealed class RecordingChannel(string channelType) : IMfaChannel
    {
        public string ChannelType { get; } = channelType;
        public string? SentCode { get; private set; }

        public Task<MfaSendResult> SendAsync(MfaChallenge challenge, string plaintextCode, CancellationToken cancellationToken = default)
        {
            SentCode = plaintextCode;
            return Task.FromResult(MfaSendResult.Sent);
        }

        public Task<MfaVerifyResult> VerifyAsync(MfaChallenge challenge, string submittedCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(MfaVerifyResult.Valid);
    }

    private sealed class TotpStyleChannel : IMfaChannel
    {
        public string ChannelType => "Totp";
        public string? SentCode { get; private set; }

        public Task<MfaSendResult> SendAsync(MfaChallenge challenge, string plaintextCode, CancellationToken cancellationToken = default)
        {
            SentCode = plaintextCode;
            return Task.FromResult(MfaSendResult.Sent);
        }

        public Task<MfaVerifyResult> VerifyAsync(MfaChallenge challenge, string submittedCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(MfaVerifyResult.Valid);
    }

    private sealed class InMemoryStore : IMfaCodeStore
    {
        private readonly Dictionary<string, MfaChallenge> _items = new();

        public Task StoreAsync(MfaChallenge challenge, CancellationToken cancellationToken = default)
        {
            _items[challenge.ChallengeId] = challenge;
            return Task.CompletedTask;
        }

        public Task<MfaChallenge?> GetAsync(string challengeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(challengeId, out var v) ? v : null);

        public Task UpdateAsync(MfaChallenge challenge, CancellationToken cancellationToken = default)
        {
            _items[challenge.ChallengeId] = challenge;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string challengeId, CancellationToken cancellationToken = default)
        {
            _items.Remove(challengeId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
