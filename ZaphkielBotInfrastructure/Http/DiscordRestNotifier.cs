using System.Net.Http.Json;
using ZaphkielBotCore.Abstractions;

namespace ZaphkielBotInfrastructure.Http;

public sealed class DiscordRestNotifier(HttpClient http) : IDiscordNotifier
{
    public async Task PostMessageAsync(string channelId, string content, CancellationToken cancellationToken)
    {
        // allowed_mentions with an empty parse list: relayed text can never ping @everyone, roles or users.
        using var response = await http.PostAsJsonAsync(
            $"channels/{Uri.EscapeDataString(channelId)}/messages",
            new { content, allowed_mentions = new { parse = Array.Empty<string>() } },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Discord API returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
    }
}
