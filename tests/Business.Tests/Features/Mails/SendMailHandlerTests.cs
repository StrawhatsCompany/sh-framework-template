using Business.Features.Mails.Send;
using Business.Providers;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using Business.Providers.Mail.Dtos;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Tests.Features.Mails;

public class SendMailHandlerTests
{
    [Fact]
    public async Task Success_path_returns_Result_Success()
    {
        var (handler, provider) = BuildHandler();
        provider.SendAsync(Arg.Any<SendMailContract.Request>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new SendMailContract.Response("id", "SENT")));

        var result = await handler.HandleAsync(BuildCommand());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Failure_path_propagates_provider_errors()
    {
        var (handler, provider) = BuildHandler();
        var failure = ProviderResult.Failure<SendMailContract.Response>(ResultCode.Failure);
        provider.SendAsync(Arg.Any<SendMailContract.Request>(), Arg.Any<CancellationToken>())
            .Returns(failure);

        var result = await handler.HandleAsync(BuildCommand());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Sets_ProviderType_to_Smtp_explicitly()
    {
        var captured = await CaptureCredential();

        Assert.NotNull(captured);
        Assert.Equal(MailProviderType.Smtp, captured.ProviderType);
    }

    [Fact]
    public async Task Propagates_credentials_from_MailOptions_to_factory()
    {
        var captured = await CaptureCredential();

        Assert.Equal("user@example.com", captured!.UserName);
        Assert.Equal("p@ssw0rd", captured.Password);
    }

    [Fact]
    public async Task Leaves_credentials_null_when_MailOptions_omits_them()
    {
        var captured = await CaptureCredential(noCredentials: true);

        Assert.Null(captured!.UserName);
        Assert.Null(captured.Password);
    }

    private static async Task<MailProviderCredential?> CaptureCredential(
        bool noCredentials = false)
    {
        MailProviderCredential? captured = null;
        var provider = Substitute.For<IMailProvider>();
        var factory = Substitute.For<IProviderFactory<MailProviderCredential, IMailProvider>>();
        factory.Create(Arg.Do<MailProviderCredential>(c => captured = c)).Returns(provider);
        provider.SendAsync(Arg.Any<SendMailContract.Request>(), Arg.Any<CancellationToken>())
            .Returns(ProviderResult.Success(new SendMailContract.Response("id", "SENT")));
        var handler = new SendMailHandler(factory, BuildOptions(noCredentials));

        await handler.HandleAsync(BuildCommand());

        return captured;
    }

    private static (SendMailHandler handler, IMailProvider provider) BuildHandler()
    {
        var provider = Substitute.For<IMailProvider>();
        var factory = Substitute.For<IProviderFactory<MailProviderCredential, IMailProvider>>();
        factory.Create(Arg.Any<MailProviderCredential>()).Returns(provider);
        return (new SendMailHandler(factory, BuildOptions()), provider);
    }

    private static IOptions<MailOptions> BuildOptions(bool noCredentials = false) =>
        Options.Create(new MailOptions
        {
            HostName = "smtp.test",
            Port = 25,
            UseSsl = false,
            FromAddress = "noreply@example.com",
            FromName = "Test",
            Username = noCredentials ? null : "user@example.com",
            Password = noCredentials ? null : "p@ssw0rd",
        });

    private static SendMailCommand BuildCommand() => new()
    {
        To = [new MailAddress("recipient@example.com", "Recipient")],
        Subject = "subject",
        HtmlBody = "<p>body</p>",
    };
}
