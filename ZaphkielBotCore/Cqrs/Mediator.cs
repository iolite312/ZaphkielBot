using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ZaphkielBotCore.Cqrs;

public sealed class Mediator(IServiceProvider services) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> CommandDispatchers = new();
    private static readonly ConcurrentDictionary<Type, object> QueryDispatchers = new();

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatcher = (CommandDispatcher<TResponse>)CommandDispatchers.GetOrAdd(command.GetType(), static type =>
            Activator.CreateInstance(typeof(CommandDispatcher<,>).MakeGenericType(type, typeof(TResponse)))!);

        return dispatcher.Dispatch(command, services, cancellationToken);
    }

    public Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dispatcher = (QueryDispatcher<TResponse>)QueryDispatchers.GetOrAdd(query.GetType(), static type =>
            Activator.CreateInstance(typeof(QueryDispatcher<,>).MakeGenericType(type, typeof(TResponse)))!);

        return dispatcher.Dispatch(query, services, cancellationToken);
    }

    private abstract class CommandDispatcher<TResponse>
    {
        public abstract Task<TResponse> Dispatch(object command, IServiceProvider services, CancellationToken ct);
    }

    private sealed class CommandDispatcher<TCommand, TResponse> : CommandDispatcher<TResponse>
        where TCommand : ICommand<TResponse>
    {
        public override Task<TResponse> Dispatch(object command, IServiceProvider services, CancellationToken ct) =>
            services.GetRequiredService<ICommandHandler<TCommand, TResponse>>().Handle((TCommand)command, ct);
    }

    private abstract class QueryDispatcher<TResponse>
    {
        public abstract Task<TResponse> Dispatch(object query, IServiceProvider services, CancellationToken ct);
    }

    private sealed class QueryDispatcher<TQuery, TResponse> : QueryDispatcher<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public override Task<TResponse> Dispatch(object query, IServiceProvider services, CancellationToken ct) =>
            services.GetRequiredService<IQueryHandler<TQuery, TResponse>>().Handle((TQuery)query, ct);
    }
}
