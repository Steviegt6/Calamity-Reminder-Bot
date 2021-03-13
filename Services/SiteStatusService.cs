using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class SiteStatusService : BaseConfigService
	{
		private Timer _updateCacheTimer;

		public SiteStatusService(IServiceProvider services) : base(services)
		{
			_updateCacheTimer = new Timer(e =>
			                              {
				                              foreach (GuildConfig config in _guildConfigService.GetAllConfigs())
				                              foreach (SiteStatus status in config.SiteStatuses)
					                              status.Revalidate();
			                              }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
		}

		public bool HasName(string name) => _guildConfig.SiteStatuses.Any(x => x.Name.EqualsIgnoreCase(name));

		public bool HasAddress(string addr) => _guildConfig.SiteStatuses.Any(x => x.Address.EqualsIgnoreCase(addr));

		public IEnumerable<SiteStatus> AllSiteStatuses()
		{
			for (int i = 0; i < _guildConfig.SiteStatuses.Count; i++) yield return _guildConfig.SiteStatuses[i];
		}

		public (string cachedResult, string url) GetCachedResult(string key)
		{
			SiteStatus status = _guildConfig.SiteStatuses.FirstOrDefault(x => x.Name.EqualsIgnoreCase(key));
			return status == null ? (null, null) : (status.CachedResult, status.Address);
		}

		~SiteStatusService()
		{
			_updateCacheTimer.Dispose();
			_updateCacheTimer = null;
		}

		public async Task UpdateAsync()
		{
			foreach (GuildConfig config in _guildConfigService.GetAllConfigs()) await UpdateForConfig(config);
		}

		public Task UpdateForConfig(GuildConfig config)
		{
			IEnumerable<SiteStatus> needsValidation = config.SiteStatuses.Where(x => x.StatusCode == SiteStatusCode.Unknown);
			foreach (SiteStatus siteStatus in needsValidation) siteStatus.Revalidate();
			return Task.CompletedTask;
		}
	}
}