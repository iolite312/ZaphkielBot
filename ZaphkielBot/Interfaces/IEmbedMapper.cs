using Discord;
using ZaphkielShared.Models;

namespace ZaphkielBot.Interfaces;

public interface IEmbedMapper
{
    Embed Build(EmbedMessage message);
}
