using FluentValidation;
using SITAG.Application.Admin.Commands;

namespace SITAG.Application.Admin.Validators;

internal sealed class UpdateTenantStatusCommandValidator : AbstractValidator<UpdateTenantStatusCommand>
{
    public UpdateTenantStatusCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}
