using Microsoft.Extensions.DependencyInjection;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Messaging;

namespace ZaphkielCore;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddZaphkielCore(this IServiceCollection services)
    {
        services.AddMediator();
        services.AddHandlersFromAssembly(typeof(CoreServiceCollectionExtensions).Assembly);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IQueuedCommandDispatcher, QueuedCommandDispatcher>();
        return services;
    }
}
