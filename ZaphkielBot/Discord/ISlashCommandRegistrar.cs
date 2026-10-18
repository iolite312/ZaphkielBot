namespace ZaphkielBot.Discord;

public interface ISlashCommandRegistrar
{
    /// <summary>Publishes the slash commands defined by the modules to Discord (to one server if configured, else globally).</summary>
    Task RegisterAsync(CancellationToken cancellationToken);
}
