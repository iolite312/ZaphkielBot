namespace ZaphkielShared.Messaging;

public readonly record struct Unit
{
    public static readonly Unit Value = default;
}

public interface ICommand<TResponse>;

public interface IQuery<TResponse>;

public interface IQueuedCommand : ICommand<Unit>;
