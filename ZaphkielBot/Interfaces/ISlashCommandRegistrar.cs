namespace ZaphkielBot.Discord;

public interface ISlashCommandRegistrar
{
    Task RegisterAsync(CancellationToken cancellationToken);
}
