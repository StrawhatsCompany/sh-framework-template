using Business.Providers.Mail;
using Providers.Mail.Smtp;

namespace Providers.Mail.Tests.Smtp;

public class SmtpClientPoolTests
{
    [Fact]
    public void Key_changes_when_host_changes()
    {
        var a = SmtpClientPool.KeyFor(Cred(host: "smtp.a"));
        var b = SmtpClientPool.KeyFor(Cred(host: "smtp.b"));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Key_changes_when_username_changes()
    {
        var anon = SmtpClientPool.KeyFor(Cred(user: null));
        var auth = SmtpClientPool.KeyFor(Cred(user: "alice"));

        Assert.NotEqual(anon, auth);
    }

    [Fact]
    public void Key_is_stable_for_same_credential()
    {
        var first = SmtpClientPool.KeyFor(Cred());
        var second = SmtpClientPool.KeyFor(Cred());

        Assert.Equal(first, second);
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var pool = new SmtpClientPool();

        pool.Dispose();
        pool.Dispose();
    }

    [Fact]
    public async Task SendAsync_throws_after_Dispose()
    {
        var pool = new SmtpClientPool();
        pool.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            pool.SendAsync(Cred(), new MimeKit.MimeMessage()));
    }

    private static MailProviderCredential Cred(string host = "smtp.test", string? user = null) =>
        new()
        {
            ProviderType = MailProviderType.Smtp,
            HostName = host,
            Port = 25,
            UseSsl = false,
            UserName = user,
            Password = user is null ? null : "pw",
        };
}
