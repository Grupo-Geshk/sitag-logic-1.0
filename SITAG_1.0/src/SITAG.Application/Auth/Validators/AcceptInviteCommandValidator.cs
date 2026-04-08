using FluentValidation;
using SITAG.Application.Auth.Commands;

namespace SITAG.Application.Auth.Validators;

internal sealed class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.RawToken).NotEmpty();

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .When(x => x.Phone is not null);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
