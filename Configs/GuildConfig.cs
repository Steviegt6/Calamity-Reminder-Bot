﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot.Configs
{
	public sealed class GuildConfig
	{
		public ulong GuildId;
		public IList<SiteStatus> SiteStatuses = new List<SiteStatus>();

		public GuildConfig(SocketGuild guild)
		{
			if (guild != null)
			{
				GuildId = guild.Id;
			}
		}

		public async Task Update(GuildConfigService service)
		{
			await service.UpdateCacheForConfig(this);
			await service.WriteGuildConfig(this);
		}
	}
}
