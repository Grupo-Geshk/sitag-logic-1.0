using FluentValidation;
using SITAG.Application.Auth.Commands;

namespace SITAG.Application.Auth.Validators;

internal sealed class RegisterAdminCommandValidator : AbstractValidator<RegisterAdminCommand>
{
    public RegisterAdminCommandValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
