using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SITAG.Api.Tests.Infrastructure;

namespace SITAG.Api.Tests.Security;

/// <summary>
/// Verifies that /admin/** endpoints are blocked for non-AdminSistema roles.
/// REQ-USER-02
/// </summary>
[Collection("Integration")]
public sealed class RoleEnforcementTests
{
    private readonly SitagWebApplicationFactory _factory;

    public RoleEnforcementTests(SitagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClientWithRole(string role)
    {
        var client = _factory.CreateClient();
        var token = JwtTokenHelper.GenerateToken(Guid.NewGuid(), Guid.NewGuid(), role: role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("GET", "/admin/users")]
    [InlineData("GET", "/admin/tenants")]
    [InlineData("GET", "/admin/statistics")]
    public async Task AdminEndpoints_ProductorRole_Returns403(string method, string path)
    {
        var client = ClientWithRole("Productor");
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("GET", "/admin/users")]
    [InlineData("GET", "/admin/tenants")]
    [InlineData("GET", "/admin/statistics")]
    public async Task AdminEndpoints_AdminSistemaRole_Returns2xx(string method, string path)
    {
        var client = ClientWithRole("AdminSistema");
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await client.SendAsync(request);
        ((int)response.StatusCode).Should().BeLessThan(400,
            because: $"{path} should be accessible by AdminSistema");
    }
}
