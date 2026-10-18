using Microsoft.Extensions.DependencyInjection;
using ZaphkielBotCore.Cqrs;

namespace ZaphkielBot.Tests.Queries;

public class MediatorTests
{
    private sealed record Ping(string Text) : ICommand<string>;
    private sealed record Ask(int N) : IQuery<int>;

    private sealed class PingHandler : ICommandHandler<Ping, string>
    {
        public Task<string> Handle(Ping command, CancellationToken ct) => Task.FromResult(command.Text + "!");
    }

    private sealed class AskHandler : IQueryHandler<Ask, int>
    {
        public Task<int> Handle(Ask query, CancellationToken ct) => Task.FromResult(query.N * 2);
    }

    private static IMediator Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ICommandHandler<Ping, string>, PingHandler>();
        services.AddScoped<IQueryHandler<Ask, int>, AskHandler>();
        configure?.Invoke(services);
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Routes_commands_and_queries_to_their_handlers()
    {
        var mediator = Build();

        Assert.Equal("hi!", await mediator.Send(new Ping("hi")));
        Assert.Equal(42, await mediator.Query(new Ask(21)));
    }

    [Fact]
    public async Task Missing_handler_is_reported()
    {
        var mediator = new ServiceCollection().AddScoped<IMediator, Mediator>()
            .BuildServiceProvider().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new Ping("x")));
    }

    [Fact]
    public void Assembly_scan_registers_every_core_handler()
    {
        var services = new ServiceCollection().AddCqrs(typeof(ZaphkielBotCore.CoreServiceCollectionExtensions).Assembly);

        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<ZaphkielBotCore.Commands.CreateIncidentCommand, Unit>));
        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<ZaphkielBotCore.Commands.ResolveIncidentCommand, Unit>));
        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<ZaphkielBotCore.Commands.RecordGitHubCommentCommand, Unit>));
        Assert.Contains(services, d => d.ServiceType == typeof(IQueryHandler<ZaphkielBotCore.Queries.GetAnalyticsQuery, ZaphkielBotCore.Queries.AnalyticsReport>));
        Assert.Contains(services, d => d.ServiceType == typeof(IQueryHandler<ZaphkielBotCore.Queries.GetIncidentQuery, ZaphkielBotCore.Queries.IncidentView?>));
    }
}
