using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SITAG.Application.Common.Behaviours;

namespace SITAG.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR: discovers all IRequestHandler<,> in this assembly
        // Infrastructure assembly is also passed at startup via AddInfrastructure
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Validation pipeline: runs before every handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        // FluentValidation: discovers all AbstractValidator<T> in this assembly
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
