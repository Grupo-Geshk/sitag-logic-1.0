using SITAG.Application.Common.Interfaces;

namespace SITAG.Infrastructure.Identity;

public sealed class PasswordHasher : IPasswordHasher
{
    // work factor 12 is the current OWASP recommendation
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
