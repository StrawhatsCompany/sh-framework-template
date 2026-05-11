using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Business.Libraries.Authentication.Jwt;

public static class JwtExtensions
{
    /// <summary>
    /// Wires JWT bearer authentication using <see cref="JwtOptions"/> bound from the
    /// <c>Authentication:Jwt</c> configuration section. Adds <see cref="IJwtTokenIssuer"/>
    /// so consumer handlers can mint tokens.
    /// </summary>
    public static SHAuthenticationBuilder AddJwt(this SHAuthenticationBuilder builder)
    {
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();

        builder.AuthenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
        {
            // Reach into IOptions at handler-config time so the singleton-resolved JwtOptions
            // values are the same ones the issuer uses to mint tokens.
            var options = builder.Services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<JwtOptions>>()
                .Value;

            jwt.RequireHttpsMetadata = true;
            jwt.SaveToken = false;
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                ClockSkew = options.ClockSkew,
            };
        });

        return builder;
    }
}
