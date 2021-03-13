﻿using System;
using System.IO;
using System.Resources;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Services;

namespace tModloaderDiscordBot
{
	public class Program
	{
		internal static IUser BotOwner;

		public static bool Ready;
		private DiscordSocketClient _client;
		private CommandService _commandService;
		private LoggingService _loggingService;
		//private ModService _modService;
		private IServiceProvider _services;

		public static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();
		//private ReactionRoleService _reactionRoleService;

		private async Task StartAsync()
		{
			IServiceCollection BuildServiceCollection()
			{
				IServiceCollection serviceCollection =
					new ServiceCollection()
						.AddSingleton(_client)
						.AddSingleton(_commandService)
						.AddSingleton<UserHandlerService>()
						.AddSingleton<CommandHandlerService>()
						//.AddSingleton<HastebinService>()
						//.AddSingleton<RecruitmentChannelService>()
						//.AddSingleton<BanAppealChannelService>()
						//.AddSingleton<ReactionRoleService>()
						.AddSingleton(new ResourceManager("tModloaderDiscordBot.Properties.Resources",
							GetType().Assembly))
						.AddSingleton<LoggingService>()
						.AddSingleton<GuildConfigService>()
						.AddSingleton<SiteStatusService>()
						.AddSingleton<GuildTagService>()
						.AddSingleton<PermissionService>();
						//.AddSingleton<ModService>()

				//foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
				//	.SelectMany(x => x.GetTypes())
				//	.Where(x =>
				//		x.IsAssignableFrom(typeof(IBotService))
				//		&& x.IsClass
				//		&& !x.IsAbstract))
				//{
				//	serviceCollection.AddSingleton(Activator.CreateInstance(type));
				//}

				return serviceCollection;
			}

			// How to use resources:
			//_services.GetRequiredService<ResourceManager>().GetString("key")

			_client = new DiscordSocketClient(new DiscordSocketConfig
			{
				AlwaysDownloadUsers = true,
				GatewayIntents =
					GatewayIntents.Guilds |
					GatewayIntents.GuildMembers |
					GatewayIntents.GuildMessageReactions |
					GatewayIntents.GuildMessages |
					GatewayIntents.GuildVoiceStates,
				LogLevel = LogSeverity.Verbose
			});
			_commandService = new CommandService(new CommandServiceConfig
			{
				DefaultRunMode = RunMode.Async,
				CaseSensitiveCommands = false
			});

			_services = BuildServiceCollection().BuildServiceProvider();
			await _services.GetRequiredService<CommandHandlerService>().InitializeAsync();
			_loggingService = _services.GetRequiredService<LoggingService>();
			_loggingService.InitializeAsync();
			//_modService = _services.GetRequiredService<ModService>();
			//_reactionRoleService = _services.GetRequiredService<ReactionRoleService>();
			//_services.GetRequiredService<HastebinService>();
			//_services.GetRequiredService<RecruitmentChannelService>();

			_client.Ready += ClientReady;
			//_client.GuildAvailable += ClientGuildAvailable;
			_client.LatencyUpdated += ClientLatencyUpdated;

			Console.Title = $@"tModLoader Bot - {DateTime.Now}";
			await Console.Out.WriteLineAsync("https://discordapp.com/api/oauth2/authorize?client_id=&scope=bot");
			await Console.Out.WriteLineAsync($"Start date: {DateTime.Now}");
			//#if TESTBOT
			await _client.LoginAsync(TokenType.Bot, await File.ReadAllTextAsync("token.txt"));
			//#else
			//			await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TmlBotToken"));
			//#endif
			await _client.StartAsync();

			await Task.Delay(-1);
		}

		private async Task ClientLatencyUpdated(int i, int j)
		{
			UserStatus newUserStatus = UserStatus.Online;

			switch (_client.ConnectionState)
			{
				case ConnectionState.Disconnected:
					newUserStatus = UserStatus.DoNotDisturb;
					break;

				case ConnectionState.Connecting:
					newUserStatus = UserStatus.Idle;
					break;
			}

			await _client.SetStatusAsync(newUserStatus);
		}

		private async Task ClientReady()
		{
			Ready = false;
			await _client.SetGameAsync("Bot is starting");
			await _client.SetStatusAsync(UserStatus.Invisible);

			BotOwner = (await _client.GetApplicationInfoAsync()).Owner;

			await _services.GetRequiredService<GuildConfigService>().SetupAsync();
			await _services.GetRequiredService<SiteStatusService>().UpdateAsync();
			//await _modService.Initialize();
			//await _modService.Maintain(_client);
			//await _reactionRoleService.Maintain(_client);
			//Odd bug, doesn't work here on tmod server. Check if this works once discord.net updates.
			//await _services.GetRequiredService<RecruitmentChannelService>().SetupAsync();

			await _loggingService.Log(new LogMessage(LogSeverity.Info, "ClientReady", "Done."));
			await _client.SetGameAsync("Sorting through clipboards...");
			await ClientLatencyUpdated(_client.Latency, _client.Latency);
			Ready = true;
		}

		/*private async Task ClientGuildAvailable(SocketGuild arg)
		{
			//await _services.GetRequiredService<RecruitmentChannelService>().SetupAsync();
			//_services.GetRequiredService<BanAppealChannelService>().Setup();
		}*/
	}
}