using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using Xunit;

namespace AvailabilityService.Tests;

public sealed class AvailabilityAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const string SigningKey = "01234567890123456789012345678901";

    private readonly WebApplicationFactory<Program> _factory;

    public AvailabilityAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "",
                    ["Auth:LocalIssuer"] = Issuer,
                    ["Auth:LocalAudience"] = Audience,
                    ["Auth:LocalSigningKey"] = SigningKey
                });
            });
        });
    }

    [Fact]
    public async Task Slots_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var response = await client.GetAsync($"/availability/slots?tenantId={tenantId}&serviceId={serviceId}&date=2026-03-07");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}
