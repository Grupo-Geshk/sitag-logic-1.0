using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Queries;

public sealed record InviteInfoDto(string TenantName, string Email);

/// <summary>
/// Validates an invite token and returns tenant + email info so the
/// registration form can be pre-filled. Does not consume the token.
/// </summary>
public sealed record ValidateInviteQuery(string RawToken) : IRequest<InviteInfoDto>;

public sealed class ValidateInviteQueryHandler : IRequestHandler<ValidateInviteQuery, InviteInfoDto>
{
    private readonly IApplicationDbContext _db;

    public ValidateInviteQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<InviteInfoDto> Handle(ValidateInviteQuery req, CancellationToken ct)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(req.RawToken)));

        var invite = await _db.UserInvites
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct)
            ?? throw new KeyNotFoundException("El enlace de invitación no es válido.");

        if (!invite.IsValid)
            throw new InvalidOperationException(
                invite.IsExpired   ? "El enlace de invitación ha expirado." :
                invite.IsAccepted  ? "Este enlace de invitación ya fue utilizado." :
                                     "El enlace de invitación no es válido.");

        return new InviteInfoDto(invite.Tenant.Name, invite.Email);
    }
}
