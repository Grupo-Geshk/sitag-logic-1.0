using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Application.Admin.Queries;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IApplicationDbContext _db;
    public GetUserByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<UserDto> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().Include(u => u.Tenant)
            .Where(u => u.Id == req.UserId && u.DeletedAt == null)
            .Select(u => new UserDto(
                u.Id, u.TenantId, u.Tenant.Name,
                u.Email, u.FirstName, u.LastName, u.Phone,
                u.Role, u.IsActive, u.MustChangePassword,
                u.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"User {req.UserId} not found.");
        return u;
    }
}
