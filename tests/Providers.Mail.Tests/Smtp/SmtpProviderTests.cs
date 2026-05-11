using System.Net.Mime;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using Business.Providers.Mail.Dtos;
using MimeKit;
using Providers.Mail.Smtp;

namespace Providers.Mail.Tests.Smtp;

public class SmtpProviderTests
{
    [Fact]
    public async Task SendAsync_returns_NeedBody_when_body_is_empty_and_does_not_touch_pool()
    {
        var pool = Substitute.For<ISmtpClientPool>();
        var provider = new SmtpProvider(pool, Cred());

        var result = await provider.SendAsync(Request(MailBody.Instance()));

        Assert.False(result.IsSuccess);
        Assert.Equal(MailProviderResultCode.NeedBody.Code, result.Code);
        await pool.DidNotReceiveWithAnyArgs().SendAsync(default!, default!);
    }

    [Fact]
    public async Task SendAsync_forwards_to_pool_with_the_provider_credential()
    {
        var pool = Substitute.For<ISmtpClientPool>();
        var credential = Cred();
        var provider = new SmtpProvider(pool, credential);

        var result = await provider.SendAsync(Request(MailBody.Instance().WithHtmlBody("<p>hi</p>")));

        Assert.True(result.IsSuccess);
        await pool.Received(1).SendAsync(credential, Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_attaches_files_when_present()
    {
        var pool = Substitute.For<ISmtpClientPool>();
        MimeMessage? captured = null;
        await pool.SendAsync(Arg.Any<MailProviderCredential>(), Arg.Do<MimeMessage>(m => captured = m), Arg.Any<CancellationToken>());
        var provider = new SmtpProvider(pool, Cred());

        var body = MailBody.Instance().WithHtmlBody("<p>x</p>");
        body.AddAttachment(new MailAttachment(
            FileName: "report.txt",
            ContentType: new System.Net.Mime.ContentType("text/plain"),
            File: new MemoryStream("hello"u8.ToArray())));

        await provider.SendAsync(Request(body));

        Assert.NotNull(captured);
        Assert.True(captured.Attachments.Any());
    }

    private static MailProviderCredential Cred() => new()
    {
        ProviderType = MailProviderType.Smtp,
        HostName = "smtp.test",
        Port = 25,
        UseSsl = false,
    };

    private static SendMailContract.Request Request(MailBody body) =>
        new(
            From: new MailAddress("from@example.com", "From"),
            To: [new MailAddress("to@example.com", "To")],
            Subject: "subject",
            MailBody: body);
}
