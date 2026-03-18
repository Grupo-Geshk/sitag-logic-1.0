using FluentValidation;
using SITAG.Application.Admin.Commands;

namespace SITAG.Application.Admin.Validators;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
