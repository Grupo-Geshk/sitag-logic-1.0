using FluentAssertions;
using SITAG.Application.Animals.Commands;
using SITAG.Application.Tests.Helpers;
using SITAG.Domain.Enums;

namespace SITAG.Application.Tests.Animals;

public sealed class AnimalEventCommandTests
{
    [Fact]
    public async Task CreateEvent_BirthWithOffspring_CreatesOffspringAnimal()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var parent = SeedData.SeedAnimal(db, user.TenantId, farm.Id, "PARENT01");

        var handler = new CreateAnimalEventHandler(db, user);
        var offspring = new OffspringData("OFFSPRING01", "M", "Calf", null, 30m);
        var cmd = new CreateAnimalEventCommand(
            parent.Id, AnimalEventType.Nacimiento, DateTimeOffset.UtcNow,
            null, null, null, null, null, offspring);

        await handler.Handle(cmd, CancellationToken.None);

        var newborn = db.Animals.FirstOrDefault(a => a.TagNumber == "OFFSPRING01");
        newborn.Should().NotBeNull();
        newborn!.ParentId.Should().Be(parent.Id);
        newborn.TenantId.Should().Be(user.TenantId);
        newborn.FarmId.Should().Be(farm.Id);
    }

    [Fact]
    public async Task CreateEvent_BirthWithDuplicateOffspringTag_ThrowsInvalidOperation()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var parent = SeedData.SeedAnimal(db, user.TenantId, farm.Id, "PARENT01");
        SeedData.SeedAnimal(db, user.TenantId, farm.Id, "EXISTING_TAG");

        var handler = new CreateAnimalEventHandler(db, user);
        var offspring = new OffspringData("EXISTING_TAG", "F", null, null, null);
        var cmd = new CreateAnimalEventCommand(
            parent.Id, AnimalEventType.Nacimiento, DateTimeOffset.UtcNow,
            null, null, null, null, null, offspring);

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EXISTING_TAG*already exists*");
    }

    [Fact]
    public async Task CreateEvent_SaleWithAmount_CreatesEconomyTransaction()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var animal = SeedData.SeedAnimal(db, user.TenantId, farm.Id, "SELL01");

        var handler = new CreateAnimalEventHandler(db, user);
        var cmd = new CreateAnimalEventCommand(
            animal.Id, AnimalEventType.Venta, DateTimeOffset.UtcNow,
            null, null, "Sold at market", 1500m, null);

        await handler.Handle(cmd, CancellationToken.None);

        var txn = db.EconomyTransactions.FirstOrDefault();
        txn.Should().NotBeNull();
        txn!.Type.Should().Be(TransactionType.Ingreso);
        txn.Amount.Should().Be(1500m);
        txn.TenantId.Should().Be(user.TenantId);
    }

    [Fact]
    public async Task CreateEvent_PurchaseWithAmount_CreatesEgresoTransaction()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var animal = SeedData.SeedAnimal(db, user.TenantId, farm.Id, "BUY01");

        var handler = new CreateAnimalEventHandler(db, user);
        var cmd = new CreateAnimalEventCommand(
            animal.Id, AnimalEventType.Compra, DateTimeOffset.UtcNow,
            null, null, null, 2000m, null);

        await handler.Handle(cmd, CancellationToken.None);

        db.EconomyTransactions.First().Type.Should().Be(TransactionType.Egreso);
    }

    [Fact]
    public async Task CreateEvent_AnimalBelongsToDifferentTenant_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        var userA = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var userB = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var (_, _, farm) = SeedData.SeedBasic(db, userA.TenantId);
        var animal = SeedData.SeedAnimal(db, userA.TenantId, farm.Id);

        // Tenant B tries to create event on Tenant A's animal
        var handler = new CreateAnimalEventHandler(db, userB);
        var cmd = new CreateAnimalEventCommand(
            animal.Id, AnimalEventType.Tratamiento, DateTimeOffset.UtcNow,
            null, null, "Sneaky", null, null);

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
