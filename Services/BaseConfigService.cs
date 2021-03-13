﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Components;

namespace tModloaderDiscordBot.Services
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public abstract class BaseConfigService : BaseService
	{
		protected readonly GuildConfigService _guildConfigService;
		protected ulong _gid;
		protected GuildConfig _guildConfig;

		protected BaseConfigService(IServiceProvider services) : base(services) =>
			_guildConfigService = services.GetRequiredService<GuildConfigService>();

		public virtual void Initialize(ulong gid)
		{
			_gid = gid;
			_guildConfig = _guildConfigService.GetConfig(gid);
			_guildConfig.Initialize(_guildConfigService);
		}

		public async Task RequestConfigUpdate()
		{
			await _guildConfig.Update();
		}
	}
}