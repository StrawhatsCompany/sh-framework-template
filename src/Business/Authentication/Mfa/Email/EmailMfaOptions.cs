namespace Business.Authentication.Mfa.Email;

public sealed class EmailMfaOptions
{
    public const string SectionName = "Authentication:Mfa:Email";

    public string Subject { get; set; } = "Your verification code";

    /// <summary>
    /// Plain-text template. Placeholders: <c>{{code}}</c>, <c>{{ttlMin}}</c>.
    /// HTML rendering is intentionally minimal — richer templates can land later via a
    /// dedicated TemplateRenderer abstraction.
    /// </summary>
    public string BodyTemplate { get; set; } = "Your code is {{code}}. It expires in {{ttlMin}} minutes.";

    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(10);

    public int CodeLength { get; set; } = 6;
}
