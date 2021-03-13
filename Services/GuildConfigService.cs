using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class GuildConfigServiceSettings
	{
		private readonly string _dataDir;

		public GuildConfigServiceSettings(string baseDir = "", string dataDir = "")
		{
			BaseDir = baseDir;
			_dataDir = dataDir;
		}

		public string BaseDir { get; }
		public string DataDir => Path.Combine(BaseDir, _dataDir);

		public string GuildPath(ulong guildId) => Path.Combine(DataDir, guildId.ToString());

		public string GuildConfigPath(ulong guildId) => Path.Combine(GuildPath(guildId), "config.json");

		public bool GuildConfigExists(ulong guildId) => File.Exists(GuildConfigPath(guildId));
	}

	public class GuildConfigService
	{
		private readonly DiscordSocketClient _client;
		private readonly IDictionary<ulong, GuildConfig> _guildConfigs;
		private readonly SemaphoreSlim _semaphore;
		internal readonly GuildConfigServiceSettings Settings;

		public GuildConfigService(IServiceProvider services)
		{
			_client = services.GetRequiredService<DiscordSocketClient>();
			// using semaphore to thread lock as it allows await in asynchronous context
			_semaphore = new SemaphoreSlim(1, 1);
			Settings = new GuildConfigServiceSettings(dataDir: "data");
			_guildConfigs = new Dictionary<ulong, GuildConfig>();
		}

		public GuildConfig GetConfig(ulong id)
		{
			if (_guildConfigs.ContainsKey(id))
			{
				return _guildConfigs[id];
			}

			return null;
		}

		public IEnumerable<GuildConfig> GetAllConfigs()
		{
			foreach (KeyValuePair<ulong, GuildConfig> kvp in _guildConfigs) yield return kvp.Value;
		}

		public async Task SetupAsync()
		{
			// iterate guilds and create new configs for them

			foreach (SocketGuild guild in _client.Guilds.Where(x => !Settings.GuildConfigExists(x.Id)))
			{
				Directory.CreateDirectory(Settings.GuildPath(guild.Id));
				GuildConfig gConfig = new GuildConfig(guild);
				await WriteGuildConfig(gConfig);
			}

			await UpdateCache();
		}

		internal Task<bool> UpdateCacheForConfig(GuildConfig config)
		{
			if (_guildConfigs.ContainsKey(config.GuildId))
			{
				_guildConfigs[config.GuildId] = config;
				return Task.FromResult(true);
			}

			return Task.FromResult(_guildConfigs.TryAdd(config.GuildId, config));
		}

		internal async Task UpdateCache()
		{
			string[] filePaths = Directory.GetFiles(Settings.DataDir, "config.json", SearchOption.AllDirectories);

			foreach (string filePath in filePaths)
			{
				string json = await FileUtils.FileReadToEndAsync(_semaphore, filePath);
				GuildConfig config = JsonConvert.DeserializeObject<GuildConfig>(json);
				await UpdateCacheForConfig(config);
			}
		}

		internal async Task<GuildConfig> ReadGuildConfig(ulong guildId)
		{
			string json = await FileUtils.FileReadToEndAsync(_semaphore, Settings.GuildConfigPath(guildId));
			GuildConfig config = JsonConvert.DeserializeObject<GuildConfig>(json);
			return config;
		}

		internal async Task WriteGuildConfig(GuildConfig config)
		{
			Directory.CreateDirectory(Settings.GuildPath(config.GuildId));
			string json = JsonConvert.SerializeObject(config, Formatting.Indented);
			await FileUtils.FileWriteAsync(_semaphore, Settings.GuildConfigPath(config.GuildId), json);
		}
	}
}