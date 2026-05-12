namespace Domain.Entities.Identity;

public enum MfaFactorKind
{
    Totp = 1,
    Email = 2,
    Sms = 3,
}
