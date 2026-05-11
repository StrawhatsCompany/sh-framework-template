using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Business.Libraries.Authentication.Jwt;

public static class JwtExtensions
{
    /// <summary>
    /// Wires JWT bearer authentication using <see cref="JwtOptions"/> bound from the
    /// <c>Authentication:Jwt</c> configuration section. Adds <see cref="IJwtTokenIssuer"/>
    /// so consumer handlers can mint tokens.
    /// </summary>
    public static AuthBuilder AddJwt(this AuthBuilder builder)
    {
        builder.Services
            .AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();

        // IConfigureNamedOptions<JwtBearerOptions> bridges JwtOptions → JwtBearerOptions through
        // DI — no BuildServiceProvider() at config time.
        builder.Services.AddSingleton<IConfigureNamedOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();
        builder.AuthenticationBuilder.AddJwtBearer();

        return builder;
    }
}
