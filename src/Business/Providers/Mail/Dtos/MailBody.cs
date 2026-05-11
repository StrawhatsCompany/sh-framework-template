namespace Business.Providers.Mail.Dtos;

public class MailBody
{
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public List<MailAttachment> Attachments { get; set; } = [];

    public bool HasBody => !(string.IsNullOrEmpty(TextBody) && string.IsNullOrEmpty(HtmlBody));

    public static MailBody Instance()
    {
        return new MailBody();
    }

    public MailBody WithTextBody(string textBody)
    {
        TextBody = textBody;
        return this;
    }

    public MailBody WithHtmlBody(string htmlBody)
    {
        HtmlBody = htmlBody;
        return this;
    }

    public MailBody AddAttachment(MailAttachment attachment) {
        Attachments.Add(attachment);

        return this;
    }
}
