using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Business.Libraries.Authentication.Jwt;

/// <summary>
/// Bridges <see cref="JwtOptions"/> into the framework's <see cref="JwtBearerOptions"/>. Registered
/// as <c>IConfigureNamedOptions&lt;JwtBearerOptions&gt;</c> so DI resolves <see cref="JwtOptions"/>
/// the same way every other consumer does — no <c>BuildServiceProvider()</c> at config time.
/// </summary>
internal sealed class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;
        options.RequireHttpsMetadata = true;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey ?? string.Empty)),
            ClockSkew = jwt.ClockSkew,
        };
    }
}
