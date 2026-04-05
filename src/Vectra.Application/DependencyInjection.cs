using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Vectra.Application.Abstractions.Dispatchers;

namespace Vectra.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVectraApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Get all handler types
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IActionHandler<,>)))
            .ToList();

        // Register each handler
        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IActionHandler<,>));

            services.AddScoped(interfaceType, handlerType);
        }

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}