using FluentAssertions;
using SITAG.Application.Supplies.Commands;
using SITAG.Application.Tests.Helpers;

namespace SITAG.Application.Tests.Supplies;

public sealed class ServiceConsumptionTests
{
    [Fact]
    public async Task AddConsumption_ValidData_DeductsStockAndCreatesMovement()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var supply = SeedData.SeedSupply(db, user.TenantId, farm.Id, currentQty: 100m);
        var svc    = SeedData.SeedVetService(db, user.TenantId, farm.Id);

        var handler = new AddServiceConsumptionHandler(db, user);
        var result = await handler.Handle(
            new AddServiceConsumptionCommand(svc.Id, supply.Id, 15m),
            CancellationToken.None);

        result.Quantity.Should().Be(15m);
        db.Supplies.Find(supply.Id)!.CurrentQuantity.Should().Be(85m);
        db.SupplyMovements.Should().HaveCount(1);
        db.ServiceSupplyConsumptions.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddConsumption_InsufficientStock_ThrowsInvalidOperation()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var supply = SeedData.SeedSupply(db, user.TenantId, farm.Id, currentQty: 5m);
        var svc    = SeedData.SeedVetService(db, user.TenantId, farm.Id);

        var handler = new AddServiceConsumptionHandler(db, user);
        var act = () => handler.Handle(
            new AddServiceConsumptionCommand(svc.Id, supply.Id, 10m),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task AddConsumption_ZeroQuantity_ThrowsArgumentException()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var supply = SeedData.SeedSupply(db, user.TenantId, farm.Id);
        var svc    = SeedData.SeedVetService(db, user.TenantId, farm.Id);

        var handler = new AddServiceConsumptionHandler(db, user);
        var act = () => handler.Handle(
            new AddServiceConsumptionCommand(svc.Id, supply.Id, 0m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public async Task AddConsumption_CrossTenantService_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        var userA = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var userB = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var (_, _, farmA) = SeedData.SeedBasic(db, userA.TenantId);
        var svcA = SeedData.SeedVetService(db, userA.TenantId, farmA.Id);
        var (_, _, farmB) = SeedData.SeedBasic(db, userB.TenantId);
        var supplyB = SeedData.SeedSupply(db, userB.TenantId, farmB.Id);

        // Tenant B tries to add consumption to Tenant A's service
        var handler = new AddServiceConsumptionHandler(db, userB);
        var act = () => handler.Handle(
            new AddServiceConsumptionCommand(svcA.Id, supplyB.Id, 5m),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
