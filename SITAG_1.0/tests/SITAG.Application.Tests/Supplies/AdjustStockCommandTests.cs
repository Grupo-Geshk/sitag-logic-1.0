using FluentAssertions;
using SITAG.Application.Supplies.Commands;
using SITAG.Application.Tests.Helpers;
using SITAG.Domain.Enums;

namespace SITAG.Application.Tests.Supplies;

public sealed class AdjustStockCommandTests
{
    [Fact]
    public async Task AdjustStock_ConsumptionWithSufficientStock_DeductsQuantity()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var supply = SeedData.SeedSupply(db, user.TenantId, farm.Id, currentQty: 50m);

        var handler = new AdjustStockHandler(db, user);
        var result = await handler.Handle(
            new AdjustStockCommand(supply.Id, SupplyMovementType.Consumption, 20m, "Used"),
            CancellationToken.None);

        result.CurrentQuantity.Should().Be(30m);
        db.SupplyMovements.Should().HaveCount(1);
    }

    [Fact]
    public async Task AdjustStock_ConsumptionExceedsStock_ThrowsInvalidOperation()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var supply = SeedData.SeedSupply(db, user.TenantId, farm.Id, currentQty: 5m);

        var handler = new AdjustStockHandler(db, user);
        var act = () => handler.Handle(
            new AdjustStockCommand(supply.Id, SupplyMovementType.Consumption, 10m, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient stock.");
    }

    [Fact]
    public async Task AdjustStock_EntryType_AddsQuantity()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var supply = SeedData.SeedSupply(db, user.TenantId, farm.Id, currentQty: 50m);

        var handler = new AdjustStockHandler(db, user);
        var result = await handler.Handle(
            new AdjustStockCommand(supply.Id, SupplyMovementType.Entry, 25m, "Restocked"),
            CancellationToken.None);

        result.CurrentQuantity.Should().Be(75m);
    }

    [Fact]
    public async Task AdjustStock_CrossTenantSupply_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        var userA = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var userB = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var (_, _, farmA) = SeedData.SeedBasic(db, userA.TenantId);
        var supply = SeedData.SeedSupply(db, userA.TenantId, farmA.Id);
        SeedData.SeedBasic(db, userB.TenantId);

        var handler = new AdjustStockHandler(db, userB);
        var act = () => handler.Handle(
            new AdjustStockCommand(supply.Id, SupplyMovementType.Entry, 10m, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
