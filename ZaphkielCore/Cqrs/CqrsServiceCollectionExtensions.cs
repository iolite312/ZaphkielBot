using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ZaphkielCore.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.TryAddScoped<IMediator, Mediator>();
        return services;
    }

    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var handlerInterface in type.GetInterfaces().Where(IsHandlerInterface))
                services.AddScoped(handlerInterface, type);
        }

        return services;
    }

    private static bool IsHandlerInterface(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() is var definition &&
        (definition == typeof(ICommandHandler<,>) || definition == typeof(IQueryHandler<,>));
}
