using FluentValidation;
using SITAG.Application.Admin.Commands;

namespace SITAG.Application.Admin.Validators;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
    }
}
