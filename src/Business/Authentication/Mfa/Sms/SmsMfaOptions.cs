namespace Business.Authentication.Mfa.Sms;

public sealed class SmsMfaOptions
{
    public const string SectionName = "Authentication:Mfa:Sms";

    /// <summary>
    /// Plain-text template. Single placeholder: <c>{{code}}</c>. Keep under 160 chars to fit one
    /// SMS segment — SMS isn't free and longer messages get split (and double-billed).
    /// </summary>
    public string BodyTemplate { get; set; } = "Your code is {{code}}";

    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);

    public int CodeLength { get; set; } = 6;

    /// <summary>
    /// Cost rate limit — max SMS issuances per user per <see cref="RateLimitWindow"/>. Default
    /// 5 / hour. Hitting the limit returns <c>RateLimited</c> from <c>IssueAsync</c>.
    /// </summary>
    public int RateLimitMaxIssuesPerUser { get; set; } = 5;

    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromHours(1);
}
