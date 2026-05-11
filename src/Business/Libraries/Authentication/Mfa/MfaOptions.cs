namespace Business.Libraries.Authentication.Mfa;

public sealed class MfaOptions
{
    public const string SectionName = "Authentication:Mfa";

    /// <summary>Number of digits in the generated code. Default <c>6</c>.</summary>
    public int CodeLength { get; init; } = 6;

    /// <summary>How long a challenge stays valid after issue. Default <c>5 minutes</c>.</summary>
    public TimeSpan Expiry { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Maximum verify attempts before the challenge is consumed. Default <c>5</c>.</summary>
    public int MaxAttempts { get; init; } = 5;
}
