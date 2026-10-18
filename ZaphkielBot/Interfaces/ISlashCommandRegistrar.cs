namespace ZaphkielBot.Interfaces;

public interface ISlashCommandRegistrar
{
    Task RegisterAsync(CancellationToken cancellationToken);
}
