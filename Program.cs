﻿using System;
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
		public static void Main(string[] args)
			=> new Program().StartAsync().GetAwaiter().GetResult();

		internal static IUser BotOwner;
		private CommandService _commandService;
		private DiscordSocketClient _client;
		private IServiceProvider _services;
		private LoggingService _loggingService;

		private async Task StartAsync()
		{
			IServiceCollection BuildServiceCollection()
			{
				return new ServiceCollection()
					.AddSingleton(_client)
					.AddSingleton(_commandService)
					.AddSingleton<CommandHandlerService>()
					.AddSingleton(new ResourceManager("tModloaderDiscordBot.Properties.Resources", GetType().Assembly))
					.AddSingleton<LoggingService>()
					.AddSingleton<GuildConfigService>()
					.AddSingleton<SiteStatusService>()
					.AddSingleton<GuildTagService>();
			}

			_client = new DiscordSocketClient(new DiscordSocketConfig
			{
				AlwaysDownloadUsers = true,
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

			_client.Ready += ClientReady;
			_client.LatencyUpdated += ClientLatencyUpdated;

			Console.Title = $@"tModLoader Bot - {DateTime.Now}";
			await Console.Out.WriteLineAsync($"https://discordapp.com/api/oauth2/authorize?client_id=&scope=bot");
			await Console.Out.WriteLineAsync($"Start date: {DateTime.Now}");

			await _client.LoginAsync(TokenType.Bot, _services.GetRequiredService<ResourceManager>().GetString("token"));
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

		public static bool Ready;

		private async Task ClientReady()
		{
			Ready = false;
			await _client.SetGameAsync("Bot is starting");
			await _client.SetStatusAsync(UserStatus.Invisible);

			BotOwner = (await _client.GetApplicationInfoAsync()).Owner;

			await _services.GetRequiredService<GuildConfigService>().SetupAsync();
			await _services.GetRequiredService<SiteStatusService>().UpdateAsync();

			await _loggingService.Log(new LogMessage(LogSeverity.Info, "ClientReady", "Done."));
			await _client.SetGameAsync("Bot has started");
			await ClientLatencyUpdated(_client.Latency, _client.Latency);
			Ready = true;
		}

		// TODO make proper logging service

	}
}
