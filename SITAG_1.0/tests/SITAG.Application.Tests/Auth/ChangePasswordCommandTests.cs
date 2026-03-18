using FluentAssertions;
using Moq;
using SITAG.Application.Auth.Commands;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Tests.Helpers;

namespace SITAG.Application.Tests.Auth;

public sealed class ChangePasswordCommandTests
{
    private readonly Mock<IPasswordHasher> _hasher = new();

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_UpdatesHashAndClearsFlag()
    {
        var db = DbContextFactory.Create();
        var currentUser = new FakeCurrentUser();
        var (_, user, _) = SeedData.SeedBasic(db, currentUser.TenantId, "old_hash");
        currentUser.UserId = user.Id;

        // Mark user as must-change
        user.MustChangePassword = true;
        db.SaveChanges();

        _hasher.Setup(h => h.Verify("OldPass1", "old_hash")).Returns(true);
        _hasher.Setup(h => h.Hash("NewPass123")).Returns("new_hash");

        var handler = new ChangePasswordCommandHandler(db, currentUser, _hasher.Object);
        await handler.Handle(new ChangePasswordCommand("OldPass1", "NewPass123"), CancellationToken.None);

        user.PasswordHash.Should().Be("new_hash");
        user.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ThrowsUnauthorized()
    {
        var db = DbContextFactory.Create();
        var currentUser = new FakeCurrentUser();
        var (_, user, _) = SeedData.SeedBasic(db, currentUser.TenantId, "correct_hash");
        currentUser.UserId = user.Id;

        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var handler = new ChangePasswordCommandHandler(db, currentUser, _hasher.Object);
        var act = () => handler.Handle(new ChangePasswordCommand("Wrong1", "NewPass123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*incorrect*");
    }

    [Fact]
    public async Task ChangePassword_ShortNewPassword_ThrowsArgumentException()
    {
        var db = DbContextFactory.Create();
        var currentUser = new FakeCurrentUser();
        var (_, user, _) = SeedData.SeedBasic(db, currentUser.TenantId, "hash");
        currentUser.UserId = user.Id;

        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var handler = new ChangePasswordCommandHandler(db, currentUser, _hasher.Object);
        var act = () => handler.Handle(new ChangePasswordCommand("OldPass1", "short"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*8 characters*");
    }
}
