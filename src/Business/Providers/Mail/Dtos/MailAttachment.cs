using System.Net.Mime;

namespace Business.Providers.Mail.Dtos;

public record MailAttachment(string FileName, ContentType contentType, Stream File);