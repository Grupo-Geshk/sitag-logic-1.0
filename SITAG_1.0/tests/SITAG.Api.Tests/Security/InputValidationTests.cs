using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SITAG.Api.Tests.Infrastructure;

namespace SITAG.Api.Tests.Security;

/// <summary>
/// Verifies that malformed or missing input returns 400, not 500.
/// Guards against OWASP Top 10 A03 — Injection and A06 — Vulnerable Components.
/// </summary>
[Collection("Integration")]
public sealed class InputValidationTests
{
    private readonly SitagWebApplicationFactory _factory;

    public InputValidationTests(SitagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/login", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NullEmail_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/login",
            new { Email = (string?)null, Password = "password" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/login",
            new { Email = "not-an-email", Password = "password123" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_MalformedJson_Returns400()
    {
        var response = await _factory.CreateClient().PostAsync("/auth/login",
            new StringContent("{ invalid json }", System.Text.Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidFormat_WrongCredentials_Returns401NotException()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/login",
            new { Email = "nobody@test.com", Password = "SomePassword1" });
        // Must return 401, not 500 (no unhandled exception leak)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_EmptyToken_Returns400()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/refresh",
            new { RefreshToken = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
