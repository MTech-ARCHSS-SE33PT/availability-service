using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace AvailabilityService.Tests;

public sealed class AvailabilityAuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Issuer = "IdentityService";
    private const string Audience = "queuex-platform";
    private const string SigningKey = "iX4UrgHAFL2ELwXtwFCWKhGghe98PPEC";

    private readonly WebApplicationFactory<Program> _factory;

    public AvailabilityAuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "",
                    ["Auth:LocalIssuer"] = Issuer,
                    ["Auth:LocalAudience"] = Audience,
                    ["Auth:LocalSigningKey"] = SigningKey,
                    ["Availability:SlotDurationMinutes"] = "15",
                    ["Availability:CapacityPerSlot"] = "3",
                    ["Availability:TimeZone"] = "UTC"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbConnectionFactory>();
                services.RemoveAll<DatabaseInitializer>();
                services.RemoveAll<IAvailabilityRepository>();
                services.RemoveAll<InMemoryAvailabilityStore>();
                services.AddSingleton<InMemoryAvailabilityStore>();
                services.AddSingleton<IAvailabilityRepository, InMemoryAvailabilityRepository>();
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

    [Fact]
    public async Task AvailabilityFlow_WithValidToken_CreatesRuleAndReturnsSlots()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());

        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var createResponse = await client.PostAsJsonAsync("/availability/rules", new CreateRuleRequest
        {
            TenantId = tenantId,
            ServiceId = serviceId,
            DayOfWeek = 1,
            OperatingStartTime = "09:00",
            OperatingEndTime = "09:30"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var slotsResponse = await client.GetAsync($"/availability/slots?tenantId={tenantId}&serviceId={serviceId}&date=2026-03-09");

        Assert.Equal(HttpStatusCode.OK, slotsResponse.StatusCode);
        var payload = await slotsResponse.Content.ReadFromJsonAsync<SlotsResponse>();
        Assert.NotNull(payload);
        Assert.False(payload!.IsClosed);
        Assert.Equal(2, payload.Slots.Count);
        Assert.All(payload.Slots, slot => Assert.Equal("AVAILABLE", slot.Status));
    }

    private static string CreateToken()
    {
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
