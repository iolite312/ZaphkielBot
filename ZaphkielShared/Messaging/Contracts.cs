namespace ZaphkielShared.Messaging;

/// <summary>Result type for commands that do not return a value.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}

/// <summary>A write-side request. Handlers may change state but should return little or nothing.</summary>
public interface ICommand<TResponse>;

/// <summary>A read-side request. Handlers must not change state.</summary>
public interface IQuery<TResponse>;

/// <summary>A command that travels through an Azure Storage Queue instead of being executed in-process.</summary>
public interface IQueuedCommand : ICommand<Unit>;
