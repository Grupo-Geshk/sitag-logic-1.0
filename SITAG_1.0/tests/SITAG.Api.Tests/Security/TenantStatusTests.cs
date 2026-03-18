using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SITAG.Api.Tests.Infrastructure;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Api.Tests.Security;

/// <summary>
/// Verifies tenant billing status enforcement via TenantStatusMiddleware.
/// REQ-TENANT-04
/// </summary>
[Collection("Integration")]
public sealed class TenantStatusTests
{
    private readonly SitagWebApplicationFactory _factory;

    public TenantStatusTests(SitagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DelinquentTenant_AccessingProtectedRoute_Returns402()
    {
        var tenantId = Guid.NewGuid();
        _factory.SeedDatabase(db =>
        {
            db.Tenants.Add(new Tenant
            {
                Id           = tenantId,
                Name         = "Deadbeat Farm",
                PrimaryEmail = "bad@tenant.com",
                Status       = TenantStatus.Delinquent,
            });
            db.SaveChanges();
        });

        var token  = JwtTokenHelper.GenerateToken(Guid.NewGuid(), tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/animals");

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("TENANT_DELINQUENT");
    }

    [Fact]
    public async Task PastDueTenant_AccessingProtectedRoute_Returns200WithHeader()
    {
        var tenantId = Guid.NewGuid();
        _factory.SeedDatabase(db =>
        {
            db.Tenants.Add(new Tenant
            {
                Id           = tenantId,
                Name         = "Late Payer Farm",
                PrimaryEmail = "late@tenant.com",
                Status       = TenantStatus.PastDue,
            });
            db.SaveChanges();
        });

        var token  = JwtTokenHelper.GenerateToken(Guid.NewGuid(), tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/animals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Tenant-Status", out var values).Should().BeTrue();
        values!.First().Should().Be("PAST_DUE");
    }

    [Fact]
    public async Task ActiveTenant_AccessingProtectedRoute_Returns200NoStatusHeader()
    {
        var tenantId = Guid.NewGuid();
        _factory.SeedDatabase(db =>
        {
            db.Tenants.Add(new Tenant
            {
                Id           = tenantId,
                Name         = "Good Farm",
                PrimaryEmail = "good@tenant.com",
                Status       = TenantStatus.Active,
            });
            db.SaveChanges();
        });

        var token  = JwtTokenHelper.GenerateToken(Guid.NewGuid(), tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/animals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Tenant-Status").Should().BeFalse();
    }

    [Fact]
    public async Task DelinquentTenant_AccessingAuthRoute_IsNotBlocked()
    {
        // Auth routes bypass tenant status check — should get 401 (wrong creds), not 402
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/login",
            new { Email = "nobody@tenant.com", Password = "wrong" });

        response.StatusCode.Should().NotBe(HttpStatusCode.PaymentRequired);
    }
}
