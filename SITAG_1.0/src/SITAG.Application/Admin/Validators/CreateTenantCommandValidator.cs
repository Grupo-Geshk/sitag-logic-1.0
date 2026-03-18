using FluentValidation;
using SITAG.Application.Admin.Commands;

namespace SITAG.Application.Admin.Validators;

internal sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.OwnerFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OwnerLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OwnerPassword).NotEmpty().MinimumLength(8);
    }
}
