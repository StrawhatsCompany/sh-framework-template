using Business.Configuration;
using Domain.Entities.Configuration;

namespace Business.Tests.Configuration;

public class InMemoryServiceReferenceStoreTests
{
    [Fact]
    public async Task GetActive_returns_only_active_rows_for_the_category()
    {
        var store = new InMemoryServiceReferenceStore();
        await store.AddAsync(Reference("Mail", "Smtp", isActive: true));
        await store.AddAsync(Reference("Mail", "SendGrid", isActive: false));
        await store.AddAsync(Reference("Sms", "Twilio", isActive: true));

        var result = await store.GetActiveAsync("Mail");

        Assert.Single(result);
        Assert.Equal("Smtp", result[0].ProviderType);
    }

    [Fact]
    public async Task Category_match_is_case_insensitive()
    {
        var store = new InMemoryServiceReferenceStore();
        await store.AddAsync(Reference("Mail", "Smtp"));

        var result = await store.GetActiveAsync("mail");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByGroup_returns_matching_active_row()
    {
        var store = new InMemoryServiceReferenceStore();
        await store.AddAsync(Reference("Mail", "Smtp", group: "transactional"));
        await store.AddAsync(Reference("Mail", "Smtp", group: "marketing"));

        var result = await store.GetByGroupAsync("Mail", "marketing");

        Assert.NotNull(result);
        Assert.Equal("marketing", result.Group);
    }

    [Fact]
    public async Task GetByGroup_ignores_inactive_rows()
    {
        var store = new InMemoryServiceReferenceStore();
        await store.AddAsync(Reference("Mail", "Smtp", group: "primary", isActive: false));

        var result = await store.GetByGroupAsync("Mail", "primary");

        Assert.Null(result);
    }

    [Fact]
    public async Task Remove_drops_the_row()
    {
        var store = new InMemoryServiceReferenceStore();
        var added = await store.AddAsync(Reference("Mail", "Smtp"));

        await store.RemoveAsync(added.Id);

        Assert.Empty(await store.GetActiveAsync("Mail"));
    }

    private static ServiceReference Reference(string category, string providerType, string? group = null, bool isActive = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            Category = category,
            ProviderType = providerType,
            Group = group,
            CredentialsCipher = "cipher",
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
}
