using Discord;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Components
{
	public sealed class GuildTag
	{
		public bool IsGlobal;
		public string Name;
		public ulong OwnerId;
		public string Value;

		public bool IsOwner(ulong id) => OwnerId == id;

		public bool MatchesName(string name) => Name.EqualsIgnoreCase(name);

		public static bool IsKeyValid(string key) => Format.Sanitize(key).Equals(key) && !key.Contains(" ");
	}
}