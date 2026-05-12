namespace Domain.Entities.Identity;

public enum MfaChallengeStatus
{
    Pending = 1,
    Consumed = 2,
    Expired = 3,
    Failed = 4,
}
