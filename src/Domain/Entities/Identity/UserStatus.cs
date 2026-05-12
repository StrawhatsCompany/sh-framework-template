namespace Domain.Entities.Identity;

public enum UserStatus
{
    PendingVerification = 1,
    Active = 2,
    Disabled = 3,
    Locked = 4,
}
