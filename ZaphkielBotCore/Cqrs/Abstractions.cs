namespace ZaphkielBotCore.Cqrs;

/// <summary>Result type for commands that do not return a value.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}

/// <summary>A write-side request. Handlers may change state but should return little or nothing.</summary>
public interface ICommand<TResponse>;

/// <summary>A read-side request. Handlers must not change state.</summary>
public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}

/// <summary>Single entry point that routes commands and queries to their handlers.</summary>
public interface IMediator
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
