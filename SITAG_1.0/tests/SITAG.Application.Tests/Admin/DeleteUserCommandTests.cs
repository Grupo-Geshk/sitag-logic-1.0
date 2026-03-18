using FluentAssertions;
using SITAG.Application.Admin.Commands;
using SITAG.Application.Tests.Helpers;

namespace SITAG.Application.Tests.Admin;

public sealed class DeleteUserCommandTests
{
    [Fact]
    public async Task DeleteUser_ExistingUser_SetsDeletedAtAndDeactivates()
    {
        var db = DbContextFactory.Create();
        var (_, user, _) = SeedData.SeedBasic(db);

        var handler = new DeleteUserCommandHandler(db);
        await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        var deleted = db.Users.Find(user.Id)!;
        deleted.DeletedAt.Should().NotBeNull();
        deleted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_NonExistentId_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        SeedData.SeedBasic(db);

        var handler = new DeleteUserCommandHandler(db);
        var act = () => handler.Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteUser_AlreadyDeleted_ThrowsNotFound()
    {
        var db = DbContextFactory.Create();
        var (_, user, _) = SeedData.SeedBasic(db);
        user.DeletedAt = DateTimeOffset.UtcNow;
        db.SaveChanges();

        var handler = new DeleteUserCommandHandler(db);
        var act = () => handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
