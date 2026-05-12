namespace Domain.Entities.Identity;

public enum RefreshTokenStatus
{
    Active = 1,
    Rotated = 2,
    Revoked = 3,
}
