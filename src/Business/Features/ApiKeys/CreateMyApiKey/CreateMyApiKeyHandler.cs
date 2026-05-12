using Business.Authentication;
using Business.Authentication.ApiKeys;
using Business.Common;
using Business.Features.ApiKeys;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.ApiKeys.CreateMyApiKey;

public sealed class CreateMyApiKeyCommand : Request<CreateMyApiKeyResponse>
{
    public string Name { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
}

public sealed class CreateMyApiKeyResponse
{
    public required ApiKeyDto ApiKey { get; init; }
    /// <summary>
    /// The full plaintext token. Returned ONCE — subsequent reads expose only Prefix + Last4.
    /// </summary>
    public required string Token { get; init; }
}

public sealed class CreateMyApiKeyHandler(
    IApiKeyStore apiKeys,
    IApiKeyFactory factory,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<CreateMyApiKeyCommand, CreateMyApiKeyResponse>
{
    public override async Task<Result<CreateMyApiKeyResponse>> HandleAsync(
        CreateMyApiKeyCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure<CreateMyApiKeyResponse>(AuthResultCode.InvalidCredentials);
        }

        var name = request.Name.Trim();
        if (name.Length is < 2 or > 128)
        {
            return Result.Failure<CreateMyApiKeyResponse>(AuthResultCode.ApiKeyNameInvalid);
        }
        if (request.ExpiresAt is { } expiresAt && expiresAt <= DateTime.UtcNow)
        {
            return Result.Failure<CreateMyApiKeyResponse>(AuthResultCode.ApiKeyExpiryInPast);
        }

        var generated = factory.Generate();
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            Prefix = generated.Prefix,
            Last4 = generated.Last4,
            KeyHash = generated.KeyHash,
            ExpiresAt = request.ExpiresAt,
            Status = ApiKeyStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
        };
        await apiKeys.AddAsync(apiKey, cancellationToken);

        return Result.Success(new CreateMyApiKeyResponse
        {
            ApiKey = ApiKeyDto.From(apiKey),
            Token = generated.Plaintext,
        });
    }
}
