using FluentAssertions;
using Moq;
using SITAG.Application.Auth.Commands;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Tests.Helpers;
using SITAG.Domain.Enums;
using SITAG.Infrastructure.Persistence;

namespace SITAG.Application.Tests.Auth;

public sealed class LoginCommandTests
{
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService>   _tokens = new();

    private LoginCommandHandler CreateHandler(out SitagDbContext db, Guid? tenantId = null, string? passwordHash = null)
    {
        db = DbContextFactory.Create();
        var (tenant, user, _) = SeedData.SeedBasic(db, tenantId, passwordHash ?? "hashed_pw");
        return new LoginCommandHandler(db, _hasher.Object, _tokens.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var hash = "valid_hash";
        var handler = CreateHandler(out var db, passwordHash: hash);

        _hasher.Setup(h => h.Verify("password123", hash)).Returns(true);
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<Domain.Entities.User>()))
               .Returns(("access_token", DateTimeOffset.UtcNow.AddMinutes(15)));
        _tokens.Setup(t => t.GenerateRefreshToken())
               .Returns(("raw_refresh", "hash_refresh", DateTimeOffset.UtcNow.AddDays(30)));

        var result = await handler.Handle(new LoginCommand("admin@test.com", "password123"), CancellationToken.None);

        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("raw_refresh");
        result.MustChangePassword.Should().BeFalse();
        db.RefreshTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task Login_UserNotFound_ThrowsUnauthorized()
    {
        var handler = CreateHandler(out _);

        var act = () => handler.Handle(new LoginCommand("unknown@test.com", "password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var handler = CreateHandler(out _, passwordHash: "valid_hash");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = () => handler.Handle(new LoginCommand("admin@test.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task Login_DeactivatedUser_ThrowsUnauthorized()
    {
        var db = DbContextFactory.Create();
        var tid = Guid.NewGuid();

        db.Tenants.Add(new Domain.Entities.Tenant { Id = tid, Name = "T", PrimaryEmail = "t@t.com" });
        db.Users.Add(new Domain.Entities.User
        {
            TenantId = tid, Email = "inactive@test.com",
            PasswordHash = "hash", IsActive = false,
            FirstName = "A", LastName = "B", Role = UserRole.AdminSistema,
        });
        await db.SaveChangesAsync();

        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var handler = new LoginCommandHandler(db, _hasher.Object, _tokens.Object);
        var act = () => handler.Handle(new LoginCommand("inactive@test.com", "any"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public async Task Login_MustChangePassword_ReturnsFlagTrue()
    {
        var db = DbContextFactory.Create();
        var tid = Guid.NewGuid();

        db.Tenants.Add(new Domain.Entities.Tenant { Id = tid, Name = "T", PrimaryEmail = "t@t.com" });
        db.Users.Add(new Domain.Entities.User
        {
            TenantId = tid, Email = "newuser@test.com",
            PasswordHash = "h", IsActive = true, MustChangePassword = true,
            FirstName = "A", LastName = "B", Role = UserRole.Productor,
        });
        await db.SaveChangesAsync();

        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "h")).Returns(true);
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<Domain.Entities.User>()))
               .Returns(("tok", DateTimeOffset.UtcNow.AddMinutes(15)));
        _tokens.Setup(t => t.GenerateRefreshToken())
               .Returns(("r", "rh", DateTimeOffset.UtcNow.AddDays(30)));

        var handler = new LoginCommandHandler(db, _hasher.Object, _tokens.Object);
        var result = await handler.Handle(new LoginCommand("newuser@test.com", "pass"), CancellationToken.None);

        result.MustChangePassword.Should().BeTrue();
    }
}
