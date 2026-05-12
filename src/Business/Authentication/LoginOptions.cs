namespace Business.Authentication;

public sealed class LoginOptions
{
    public const string SectionName = "Authentication:Login";

    /// <summary>
    /// Number of consecutive failed password attempts before the user is moved to
    /// <c>UserStatus.Locked</c>. An admin (or self-service unlock flow, later) must
    /// transition the user back to Active.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;
}
