using FluentAssertions;
using SITAG.Application.Animals.Commands;
using SITAG.Application.Tests.Helpers;
using SITAG.Domain.Enums;

namespace SITAG.Application.Tests.Animals;

public sealed class MoveAnimalCommandTests
{
    [Fact]
    public async Task MoveAnimal_SameFarm_ScopeIsInterna()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var animal = SeedData.SeedAnimal(db, user.TenantId, farm.Id);
        var divisionId = Guid.NewGuid();

        var handler = new MoveAnimalHandler(db, user);
        var result = await handler.Handle(
            new MoveAnimalCommand(animal.Id, farm.Id, divisionId, null, "Intra move"),
            CancellationToken.None);

        result.Scope.Should().Be(MovementScope.Interna);
        result.ToFarmId.Should().Be(farm.Id);
        db.AnimalMovements.Should().HaveCount(1);
    }

    [Fact]
    public async Task MoveAnimal_DifferentFarm_ScopeIsEntreFincas()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var animal = SeedData.SeedAnimal(db, user.TenantId, farm.Id);

        var farm2 = new Domain.Entities.Farm { Id = Guid.NewGuid(), TenantId = user.TenantId, Name = "Farm 2" };
        db.Farms.Add(farm2);
        await db.SaveChangesAsync();

        var handler = new MoveAnimalHandler(db, user);
        var result = await handler.Handle(
            new MoveAnimalCommand(animal.Id, farm2.Id, null, null, "Cross-farm"),
            CancellationToken.None);

        result.Scope.Should().Be(MovementScope.EntreFincas);
        db.Animals.Find(animal.Id)!.FarmId.Should().Be(farm2.Id);
    }

    [Fact]
    public async Task MoveAnimal_InactiveAnimal_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        var animal = SeedData.SeedAnimal(db, user.TenantId, farm.Id, "SOLD01", AnimalStatus.Vendido);

        var handler = new MoveAnimalHandler(db, user);
        var act = () => handler.Handle(
            new MoveAnimalCommand(animal.Id, farm.Id, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found or is not active*");
    }

    [Fact]
    public async Task MoveAnimal_CrossTenantAnimal_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        var userA = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var userB = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var (_, _, farmA) = SeedData.SeedBasic(db, userA.TenantId);
        var animal = SeedData.SeedAnimal(db, userA.TenantId, farmA.Id);
        SeedData.SeedBasic(db, userB.TenantId);

        var handler = new MoveAnimalHandler(db, userB);
        var act = () => handler.Handle(
            new MoveAnimalCommand(animal.Id, Guid.NewGuid(), null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
