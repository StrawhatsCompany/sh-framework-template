using SH.Framework.Library.Cqrs.Implementation;

namespace Providers.Mail;

public class MailProviderResultCode
{
    public const string Category = "MAILPROVIDER";

    public static ResultCode NeedBody => ResultCode.Instance(1000, Category, "Mail message need at least one body (text, html) part");
}
