using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApi.Tests.Endpoints;

public class OpenApiMetadataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiMetadataTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetForecastsByCity_carries_full_metadata_in_the_openapi_document()
    {
        var operation = await GetForecastsByCityOperation();

        Assert.Equal("GetForecastsByCity", operation.GetProperty("operationId").GetString());
        Assert.Equal("Get weather forecasts for a city", operation.GetProperty("summary").GetString());

        var tags = operation.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray();
        Assert.Contains("Weather", tags);
    }

    [Fact]
    public async Task GetForecastsByCity_declares_responses_for_200_400_and_500()
    {
        var operation = await GetForecastsByCityOperation();
        var responses = operation.GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("500", out _));
    }

    private async Task<JsonElement> GetForecastsByCityOperation()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("paths")
            .GetProperty("/weather/forecasts/{city}")
            .GetProperty("get");
    }
}
