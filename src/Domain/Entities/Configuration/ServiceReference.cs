namespace Domain.Entities.Configuration;

public sealed class ServiceReference
{
    public required Guid Id { get; init; }
    public required string Category { get; init; }
    public required string ProviderType { get; init; }
    public string? Group { get; init; }
    public required string CredentialsCipher { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
