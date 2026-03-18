using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace SITAG.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID claim missing."));

    protected string CurrentUserEmail =>
        User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email")
            ?? throw new UnauthorizedAccessException("Email claim missing.");
}
