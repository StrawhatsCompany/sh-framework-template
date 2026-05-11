using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Libraries.Authentication.Mfa;

public static class MfaExtensions
{
    /// <summary>
    /// Wires the default <see cref="MfaChallengeIssuer"/> + binds <see cref="MfaOptions"/> from
    /// <c>Authentication:Mfa</c>. Channels and the code store are the consumer's responsibility:
    /// register one or more <see cref="IMfaChannel"/> implementations and exactly one
    /// <see cref="IMfaCodeStore"/> before the request pipeline starts.
    /// </summary>
    public static AuthBuilder AddMfa(this AuthBuilder builder)
    {
        builder.Services.Configure<MfaOptions>(builder.Configuration.GetSection(MfaOptions.SectionName));
        builder.Services.TryAddSingleton(TimeProvider.System);
        // Fallback in-memory store so the issuer resolves cleanly in DI. Consumer swaps with
        // their Redis/EF Core/SQL implementation via TryAddSingleton — the first registration wins.
        builder.Services.TryAddSingleton<IMfaCodeStore, InMemoryMfaCodeStore>();
        builder.Services.AddSingleton<IMfaChallengeIssuer, MfaChallengeIssuer>();
        return builder;
    }
}
