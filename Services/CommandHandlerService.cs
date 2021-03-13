using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Modules;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Services
{
	public class CommandHandlerService
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commandService;
		private readonly LoggingService _loggingService;
		private readonly IServiceProvider _services;
		private readonly GuildTagService _tagService;
		private readonly UserHandlerService _userHandlerService;

		public CommandHandlerService(IServiceProvider services)
		{
			_userHandlerService = services.GetRequiredService<UserHandlerService>();
			_commandService = services.GetRequiredService<CommandService>();
			_tagService = services.GetRequiredService<GuildTagService>();
			_loggingService = services.GetRequiredService<LoggingService>();
			_client = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			_client.MessageReceived += HandleCommand;
		}

		~CommandHandlerService()
		{
			_client.MessageReceived -= HandleCommand;
		}

		public async Task InitializeAsync()
		{
			await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
		}

		private async Task HandleCommand(SocketMessage socketMessage)
		{
			// Program is ready
			if (!Program.Ready)
			{
				return;
			}

			// Valid message, no bot, no webhook, and valid channel
			if (!(socketMessage is SocketUserMessage message)
			    || message.Author.IsBot
			    || message.Author.IsWebhook
			    || !(message.Channel is SocketTextChannel channel))
			{
				return;
			}

			SocketCommandContext context = new SocketCommandContext(_client, message);

			// Message starts with prefix
			int argPos = 0;
			if (message.Content.EqualsIgnoreCase(".")
			    || !message.HasCharPrefix('.', ref argPos)
			    || !char.IsLetter(message.Content[1]))
			{
				return;
			}

			if (!_userHandlerService.UserMatchesPrerequisites(message.Author.Id))
			{
				return;
			}

			// Execute command
			IResult result = await _commandService.ExecuteAsync(context, argPos, _services);

			// Command failed
			if (!result.IsSuccess)
			{
				// It might be a tag.
				result = await TryGettingTag(message, channel, context, result);

				if (!result.IsSuccess && !result.ErrorReason.EqualsIgnoreCase("Unknown command."))
				{
					await context.Channel.SendMessageAsync(result.ErrorReason);
				}
			}
			else
			{
				_userHandlerService.AddBasicBotCooldown(message.Author.Id);
			}
		}

		private async Task<IResult> TryGettingTag(SocketUserMessage message, SocketTextChannel channel,
			SocketCommandContext context, IResult result)
		{
			if (channel != null)
			{
				// skip prefix
				string key = message.Content.Substring(1);
				string check = Format.Sanitize(key);
				if (check.Equals(key))
				{
					await _loggingService.Log(new LogMessage(LogSeverity.Info, "CommandHandlerService",
						$"User {message.Author.FullName()} in server {channel.Guild.Name} in channel {channel.Name} attempted to get tag {key}"));
					_tagService.Initialize(context.Guild.Id);

					GuildTag tag = _tagService.GetTag(message.Author.Id, key);

					// We own this tag
					if (tag != null)
					{
						RestUserMessage msg = await channel.SendMessageAsync($"{Format.Bold($"Tag: {tag.Name}")}" +
						                                                     $"\n{tag.Value}");

						await msg.AddReactionAsync(new Emoji("❌"));
						TagModule.DeleteableTags.Add(msg.Id,
							new Tuple<ulong, ulong>(message.Author.Id, context.Message.Id));
						await message.DeleteAsync();

						_userHandlerService.AddBasicBotCooldown(message.Author.Id);
						return new ExecuteResult();
					}
					// We dont own tag, look for other people's tags

					// Look for global tags
					List<GuildTag> tags = _tagService.GetTags(key, globalTagsOnly: true).ToList();
					_userHandlerService.AddBasicBotCooldown(message.Author.Id);

					// One found, list it
					if (tags.Count == 1)
					{
						tag = tags.First();
						return await _commandService.ExecuteAsync(context, $"tag -g {tag.OwnerId} {tag.Name}",
							_services);
					}

					// More found
					if (tags.Count >= 2)
					{
						// todo find by what we sent
						return await _commandService.ExecuteAsync(context, "tag -f tags::global", _services);
					}

					string tagstr = message.Content.Substring(1);
					return await _commandService.ExecuteAsync(context, $"tag -f {tagstr}", _services);
				}
			}

			return result;
		}
	}
}