using FlightStatus.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightStatus.Tests;

public class FlightEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FlightEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(null, "2024-06-10")]
    [InlineData("", "2024-06-10")]
    [InlineData("BA493", null)]
    [InlineData("BA493", "not-a-date")]
    public async Task GetStatus_Returns400_OnInvalidInput(string flightNumber, string date)
    {
        var client = _factory.CreateClient();
        var url = $"/flights/status?flightNumber={flightNumber}&date={date}";
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_BA493_ReturnsDelayed()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/flights/status?flightNumber=BA493&date=2024-06-10");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResponse>(Options);
        Assert.Equal("BA493", result!.FlightNumber);
        Assert.Equal("Delayed", result.Status.ToString());
    }

    [Fact]
    public async Task GetStatus_XX999_ReturnsUnknown()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/flights/status?flightNumber=XX999&date=2024-06-10");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlightStatusResponse>(Options);
        Assert.Equal("XX999", result!.FlightNumber);
        Assert.Equal("Unknown", result.Status.ToString());
    }
}
