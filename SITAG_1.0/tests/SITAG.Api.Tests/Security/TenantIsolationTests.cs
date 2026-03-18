using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SITAG.Api.Tests.Infrastructure;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Api.Tests.Security;

/// <summary>
/// Verifies that data belonging to Tenant A is never visible to Tenant B.
/// REQ-INFRA-06 — tenant isolation.
/// </summary>
[Collection("Integration")]
public sealed class TenantIsolationTests
{
    private readonly SitagWebApplicationFactory _factory;

    public TenantIsolationTests(SitagWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Animals_TenantB_CannotSeeAnimalsBelongingToTenantA()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var farmAId = Guid.NewGuid();

        _factory.SeedDatabase(db =>
        {
            db.Tenants.AddRange(
                new Tenant { Id = tenantA, Name = "A", PrimaryEmail = "a@test.com", Status = TenantStatus.Active },
                new Tenant { Id = tenantB, Name = "B", PrimaryEmail = "b@test.com", Status = TenantStatus.Active });
            db.Farms.Add(new Farm { Id = farmAId, TenantId = tenantA, Name = "Farm A" });
            db.Animals.AddRange(
                new Animal { Id = Guid.NewGuid(), TenantId = tenantA, FarmId = farmAId, TagNumber = "SECRET_A001", Sex = "M" },
                new Animal { Id = Guid.NewGuid(), TenantId = tenantA, FarmId = farmAId, TagNumber = "SECRET_A002", Sex = "F" });
            db.SaveChanges();
        });

        var tokenB = JwtTokenHelper.GenerateToken(Guid.NewGuid(), tenantB);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.GetAsync("/animals");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("SECRET_A001");
        body.Should().NotContain("SECRET_A002");
    }

    [Fact]
    public async Task Farms_TenantB_CannotSeeFarmsOfTenantA()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        _factory.SeedDatabase(db =>
        {
            db.Tenants.AddRange(
                new Tenant { Id = tenantA, Name = "A", PrimaryEmail = "a@t.com", Status = TenantStatus.Active },
                new Tenant { Id = tenantB, Name = "B", PrimaryEmail = "b@t.com", Status = TenantStatus.Active });
            db.Farms.Add(new Farm { Id = Guid.NewGuid(), TenantId = tenantA, Name = "SECRET_FARM_A" });
            db.SaveChanges();
        });

        var tokenB = JwtTokenHelper.GenerateToken(Guid.NewGuid(), tenantB);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.GetAsync("/farms");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("SECRET_FARM_A");
    }

    [Fact]
    public async Task Supplies_TenantB_CannotSeeSuppliesOfTenantA()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var farmAId = Guid.NewGuid();

        _factory.SeedDatabase(db =>
        {
            db.Tenants.AddRange(
                new Tenant { Id = tenantA, Name = "A", PrimaryEmail = "a@s.com", Status = TenantStatus.Active },
                new Tenant { Id = tenantB, Name = "B", PrimaryEmail = "b@s.com", Status = TenantStatus.Active });
            db.Farms.Add(new Farm { Id = farmAId, TenantId = tenantA, Name = "Farm A" });
            db.Supplies.Add(new Supply
            {
                Id = Guid.NewGuid(), TenantId = tenantA, FarmId = farmAId,
                Name = "SECRET_SUPPLY_X", Unit = "L", CurrentQuantity = 100m, MinStockLevel = 5m,
            });
            db.SaveChanges();
        });

        var tokenB = JwtTokenHelper.GenerateToken(Guid.NewGuid(), tenantB);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.GetAsync("/insumos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("SECRET_SUPPLY_X");
    }
}
