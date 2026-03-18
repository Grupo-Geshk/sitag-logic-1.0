using FluentValidation;
using SITAG.Application.Auth.Commands;

namespace SITAG.Application.Auth.Validators;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
