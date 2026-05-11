using System.Net.Mime;

namespace Business.Providers.Mail.Dtos;

public record MailAttachment(string FileName, ContentType ContentType, Stream File);
