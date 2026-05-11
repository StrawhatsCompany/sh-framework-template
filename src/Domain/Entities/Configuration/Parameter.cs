namespace Domain.Entities.Configuration;

public sealed class Parameter
{
    public required Guid Id { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Module { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
