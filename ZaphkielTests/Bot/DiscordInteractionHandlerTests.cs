using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using ZaphkielBot.Discord;
using ZaphkielShared.Options;

namespace ZaphkielTests.Bot;

/// <summary>
/// Runs the real Discord.Net pipeline (signature check, parsing, slash command routing) with locally signed requests.
/// Only the business service behind the module is mocked, and nothing talks to Discord.
/// </summary>
public class DiscordInteractionHandlerTests
{
    private const string Timestamp = "1700000000";

    private readonly IIncidentCommandService _incidents = Substitute.For<IIncidentCommandService>();
    private readonly Ed25519PrivateKeyParameters _privateKey;
    private readonly IDiscordInteractionHandler _handler;

    public DiscordInteractionHandlerTests()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        _privateKey = (Ed25519PrivateKeyParameters)pair.Private;
        var publicKeyHex = Convert.ToHexString(((Ed25519PublicKeyParameters)pair.Public).GetEncoded());

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new DiscordOptions { PublicKey = publicKeyHex, BotToken = "unused" }));
        services.AddLogging();
        services.AddZaphkielDiscord();
        services.AddScoped(_ => _incidents); // replaces the real service: last registration wins

        _handler = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IDiscordInteractionHandler>();
    }

    private string Sign(byte[] body, string timestamp = Timestamp)
    {
        var message = Encoding.UTF8.GetBytes(timestamp).Concat(body).ToArray();
        var signer = new Ed25519Signer();
        signer.Init(true, _privateKey);
        signer.BlockUpdate(message, 0, message.Length);
        return Convert.ToHexString(signer.GenerateSignature()).ToLowerInvariant();
    }

    private Task<InteractionResult> Send(string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        return _handler.HandleAsync(Sign(body), Timestamp, body, CancellationToken.None);
    }

    /// <summary>Discord.Net refuses interactions older than 3 seconds, judged by the age encoded in the snowflake id.</summary>
    private static string FreshSnowflake() =>
        ((ulong)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1420070400000L) << 22).ToString();

    private static string Ping() => $$$"""
        {"id":"{{{FreshSnowflake()}}}","application_id":"2","type":1,"version":1,"token":"tok","app_permissions":"0",
         "entitlements":[],"authorizing_integration_owners":{},"attachment_size_limit":8388608,
         "user":{"id":"500","username":"alice","discriminator":"0","avatar":null,"global_name":"Alice"}}
        """;

    private static string SlashCommand(string subcommand, string options = "[]") => $$$"""
        {"id":"{{{FreshSnowflake()}}}","application_id":"2","type":2,"version":1,"token":"tok","channel_id":"300","channel":{"id":"300","type":0,"guild_id":"400","name":"incidents","flags":0,"last_message_id":"299","parent_id":null,"position":1,"permissions":"0","rate_limit_per_user":0,"nsfw":false,"topic":null},"guild_id":"400",
         "locale":"en-US","guild_locale":"en-US","app_permissions":"0","entitlements":[],"authorizing_integration_owners":{},"context":0,"attachment_size_limit":8388608,
         "member":{"user":{"id":"500","username":"alice","discriminator":"0","avatar":null,"global_name":"Alice"},
                   "roles":[],"permissions":"0","joined_at":"2020-01-01T00:00:00.000000+00:00","flags":0,"deaf":false,"mute":false},
         "data":{"id":"6","name":"incident","type":1,"options":[{"name":"{{{subcommand}}}","type":1,"options":{{{options}}}}]}}
        """;

    [Fact]
    public async Task Ping_is_acknowledged_with_a_pong()
    {
        var result = await Send(Ping());

        Assert.Equal(InteractionOutcome.Ok, result.Outcome);
        Assert.Contains("\"type\":1", result.ResponseJson);
    }

    [Fact]
    public async Task Wrong_tampered_or_missing_signatures_are_unauthorized()
    {
        var body = Encoding.UTF8.GetBytes("""{"type":1}""");
        var signature = Sign(body);

        Assert.Equal(InteractionOutcome.Unauthorized, (await _handler.HandleAsync(signature, "1700000001", body, CancellationToken.None)).Outcome);
        Assert.Equal(InteractionOutcome.Unauthorized, (await _handler.HandleAsync(signature, Timestamp, Encoding.UTF8.GetBytes("""{"type":2}"""), CancellationToken.None)).Outcome);
        Assert.Equal(InteractionOutcome.Unauthorized, (await _handler.HandleAsync(new string('0', 128), Timestamp, body, CancellationToken.None)).Outcome);
        Assert.Equal(InteractionOutcome.Unauthorized, (await _handler.HandleAsync("not-hex", Timestamp, body, CancellationToken.None)).Outcome);
        Assert.Equal(InteractionOutcome.Unauthorized, (await _handler.HandleAsync(null, Timestamp, body, CancellationToken.None)).Outcome);
        Assert.Equal(InteractionOutcome.Unauthorized, (await _handler.HandleAsync(signature, null, body, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task Stats_command_is_routed_to_the_service_with_the_callers_identity()
    {
        _incidents.StatsAsync(Arg.Any<InvocationContext>(), Arg.Any<CancellationToken>()).Returns(BotReply.Private("all quiet"));

        var result = await Send(SlashCommand("stats"));

        Assert.Equal(InteractionOutcome.Ok, result.Outcome);
        Assert.Contains("all quiet", result.ResponseJson);
        Assert.Contains("\"flags\":64", result.ResponseJson); // ephemeral
        await _incidents.Received(1).StatsAsync(new InvocationContext("300", "500", "Alice"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Report_command_passes_its_options_to_the_service_and_replies_publicly()
    {
        _incidents.ReportAsync(default!, default, default, default, default).ReturnsForAnyArgs(BotReply.Public("INC logged"));

        var result = await Send(SlashCommand("report",
            """[{"name":"title","type":3,"value":"Checkout is down"},{"name":"severity","type":3,"value":"critical"},{"name":"description","type":3,"value":"500s"}]"""));

        Assert.Contains("INC logged", result.ResponseJson);
        Assert.DoesNotContain("\"flags\":64", result.ResponseJson);
        await _incidents.Received(1).ReportAsync(
            new InvocationContext("300", "500", "Alice"), "Checkout is down", "critical", "500s", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Optional_description_may_be_omitted()
    {
        _incidents.ReportAsync(default!, default, default, default, default).ReturnsForAnyArgs(BotReply.Public("ok"));

        await Send(SlashCommand("report", """[{"name":"title","type":3,"value":"Valid title"},{"name":"severity","type":3,"value":"low"}]"""));

        await _incidents.Received(1).ReportAsync(Arg.Any<InvocationContext>(), "Valid title", "low", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replies_never_ping_anyone()
    {
        _incidents.StatsAsync(default!, default).ReturnsForAnyArgs(BotReply.Public("hello @everyone"));

        var result = await Send(SlashCommand("stats"));

        using var json = System.Text.Json.JsonDocument.Parse(result.ResponseJson!);
        var mentions = json.RootElement.GetProperty("data").GetProperty("allowed_mentions");
        // Nothing may be parsed from the text and no explicit role/user is allowed either.
        Assert.True(!mentions.TryGetProperty("parse", out var parse) || parse.ValueKind == System.Text.Json.JsonValueKind.Null || parse.GetArrayLength() == 0);
        Assert.Equal(0, mentions.GetProperty("roles").GetArrayLength());
        Assert.Equal(0, mentions.GetProperty("users").GetArrayLength());
    }

    [Fact]
    public async Task Service_failure_still_answers_discord_in_time()
    {
        _incidents.StatsAsync(default!, default).ReturnsForAnyArgs<Task<BotReply>>(_ => throw new InvalidOperationException("boom"));

        var result = await Send(SlashCommand("stats"));

        Assert.Equal(InteractionOutcome.Ok, result.Outcome);
        Assert.Contains("went wrong", result.ResponseJson);
    }
}
