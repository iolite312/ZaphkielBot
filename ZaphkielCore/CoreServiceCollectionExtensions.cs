using Microsoft.Extensions.DependencyInjection;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Messaging;

namespace ZaphkielCore;

public static class CoreServiceCollectionExtensions
{
    /// <summary>Mediator, the shared query handlers and the queue dispatcher. Apps add their own command handlers on top.</summary>
    public static IServiceCollection AddZaphkielCore(this IServiceCollection services)
    {
        services.AddMediator();
        services.AddHandlersFromAssembly(typeof(CoreServiceCollectionExtensions).Assembly);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IQueuedCommandDispatcher, QueuedCommandDispatcher>();
        return services;
    }
}
