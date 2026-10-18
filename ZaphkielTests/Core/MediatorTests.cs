using Microsoft.Extensions.DependencyInjection;
using ZaphkielCore;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;
using ZaphkielShared.Queries;

namespace ZaphkielTests.Core;

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

    private static IMediator Build()
    {
        var services = new ServiceCollection();
        services.AddMediator();
        services.AddScoped<ICommandHandler<Ping, string>, PingHandler>();
        services.AddScoped<IQueryHandler<Ask, int>, AskHandler>();
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
        var mediator = new ServiceCollection().AddMediator().BuildServiceProvider().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new Ping("x")));
    }

    [Fact]
    public void Core_registers_the_shared_query_handlers()
    {
        var services = new ServiceCollection().AddZaphkielCore();

        Assert.Contains(services, d => d.ServiceType == typeof(IQueryHandler<GetAnalyticsQuery, AnalyticsReport>));
        Assert.Contains(services, d => d.ServiceType == typeof(IQueryHandler<GetIncidentQuery, IncidentView?>));
    }

    [Fact]
    public void Handler_scan_registers_only_handlers_from_the_given_assembly()
    {
        var services = new ServiceCollection().AddHandlersFromAssembly(typeof(ZaphkielProcessor.Handlers.CreateIncidentHandler).Assembly);

        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<CreateIncidentCommand, Unit>));
        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<ResolveIncidentCommand, Unit>));
        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<RecordGitHubCommentCommand, Unit>));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(ICommandHandler<SendDiscordMessageCommand, Unit>));
    }
}
