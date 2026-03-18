using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SITAG.Api.Tests.Infrastructure;

namespace SITAG.Api.Tests.Security;

/// <summary>
/// Verifies that protected endpoints require valid authentication.
/// REQ-INFRA-01, REQ-INFRA-02
/// </summary>
[Collection("Integration")]
public sealed class AuthenticationTests
{
    private readonly SitagWebApplicationFactory _factory;

    public AuthenticationTests(SitagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("GET", "/animals")]
    [InlineData("GET", "/farms")]
    [InlineData("GET", "/insumos")]
    [InlineData("GET", "/workers")]
    [InlineData("GET", "/economia/transacciones")]
    [InlineData("GET", "/servicios")]
    [InlineData("GET", "/dashboard")]
    public async Task ProtectedEndpoint_NoToken_Returns401(string method, string path)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401()
    {
        var expired = JwtTokenHelper.ExpiredToken(Guid.NewGuid(), Guid.NewGuid());
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expired);

        var response = await client.GetAsync("/animals");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_TamperedToken_Returns401()
    {
        var validToken = JwtTokenHelper.GenerateToken(Guid.NewGuid(), Guid.NewGuid());
        var tampered = validToken[..^5] + "XXXXX";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync("/animals");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicEndpoints_NoToken_Returns200()
    {
        var client = _factory.CreateClient();

        var healthResp = await client.GetAsync("/health");
        healthResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var versionResp = await client.GetAsync("/version");
        versionResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
