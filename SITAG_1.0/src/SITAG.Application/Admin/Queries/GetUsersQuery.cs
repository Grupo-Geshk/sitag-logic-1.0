using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Admin.Dtos;
using SITAG.Application.Common.Dtos;
using SITAG.Application.Common.Interfaces;
using SITAG.Domain.Enums;

namespace SITAG.Application.Admin.Queries;

public sealed record GetUsersQuery(
    string?   Search,
    UserRole? Role,
    Guid?     TenantId,
    bool?     IsActive,
    int PageNumber = 1,
    int PageSize   = 20) : IRequest<PagedResult<UserDto>>;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().Include(u => u.Tenant)
            .Where(u => u.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s));
        }

        if (req.Role.HasValue)
            query = query.Where(u => u.Role == req.Role.Value);

        if (req.TenantId.HasValue)
            query = query.Where(u => u.TenantId == req.TenantId.Value);

        if (req.IsActive.HasValue)
            query = query.Where(u => u.IsActive == req.IsActive.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((req.PageNumber - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(u => new UserDto(
                u.Id, u.TenantId, u.Tenant.Name,
                u.Email, u.FirstName, u.LastName, u.Phone,
                u.Role, u.IsActive, u.MustChangePassword,
                u.CreatedAt, u.LastLoginAt))
            .ToListAsync(ct);

        return new PagedResult<UserDto>(items, total, req.PageNumber, req.PageSize);
    }
}
