using FluentAssertions;
using SITAG.Application.Animals.Commands;
using SITAG.Application.Tests.Helpers;
using SITAG.Domain.Enums;

namespace SITAG.Application.Tests.Animals;

public sealed class CreateAnimalCommandTests
{
    [Fact]
    public async Task CreateAnimal_ValidData_ReturnsDto()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);

        var handler = new CreateAnimalHandler(db, user);
        var cmd = new CreateAnimalCommand("TAG001", "Bessie", "Holstein", "F",
            DateOnly.FromDateTime(DateTime.Today.AddYears(-2)), 450m,
            farm.Id, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.TagNumber.Should().Be("TAG001");
        result.TenantId.Should().Be(user.TenantId);
        result.FarmId.Should().Be(farm.Id);
        db.Animals.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAnimal_DuplicateTagSameTenant_ThrowsInvalidOperation()
    {
        var db = DbContextFactory.Create();
        var user = new FakeCurrentUser();
        var (_, _, farm) = SeedData.SeedBasic(db, user.TenantId);
        SeedData.SeedAnimal(db, user.TenantId, farm.Id, "TAG001");

        var handler = new CreateAnimalHandler(db, user);
        var cmd = new CreateAnimalCommand("TAG001", null, null, "M", null, null, farm.Id, null, null);

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TAG001*already exists*");
    }

    [Fact]
    public async Task CreateAnimal_DuplicateTagDifferentTenant_Succeeds()
    {
        var db = DbContextFactory.Create();

        // Tenant A seeds TAG001
        var userA = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var (_, _, farmA) = SeedData.SeedBasic(db, userA.TenantId);
        SeedData.SeedAnimal(db, userA.TenantId, farmA.Id, "TAG001");

        // Tenant B tries to use same tag
        var userB = new FakeCurrentUser { TenantId = Guid.NewGuid() };
        var (_, _, farmB) = SeedData.SeedBasic(db, userB.TenantId);

        var handler = new CreateAnimalHandler(db, userB);
        var cmd = new CreateAnimalCommand("TAG001", null, null, "F", null, null, farmB.Id, null, null);

        // Cross-tenant duplicate should NOT be blocked
        var result = await handler.Handle(cmd, CancellationToken.None);
        result.TagNumber.Should().Be("TAG001");
        result.TenantId.Should().Be(userB.TenantId);
    }
}
