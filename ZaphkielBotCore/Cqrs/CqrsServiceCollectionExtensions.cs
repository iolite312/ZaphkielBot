using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ZaphkielBotCore.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    /// <summary>Registers the mediator and every command/query handler found in the given assembly.</summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IMediator, Mediator>();

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
