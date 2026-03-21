using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Auth.Queries;

public sealed record GetCurrentUserQuery : IRequest<UserDto>;

public sealed class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public GetCurrentUserHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<UserDto> Handle(GetCurrentUserQuery _, CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().Include(u => u.Tenant)
            .Where(u => u.Id == _user.UserId && u.DeletedAt == null)
            .Select(u => new UserDto(
                u.Id, u.TenantId, u.Tenant.Name,
                u.Email, u.FirstName, u.LastName, u.Phone,
                u.Role, u.IsActive, u.MustChangePassword,
                u.CreatedAt, u.LastLoginAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Current user not found.");
        return u;
    }
}
