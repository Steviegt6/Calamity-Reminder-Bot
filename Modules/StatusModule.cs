﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using tModloaderDiscordBot.Components;
using tModloaderDiscordBot.Preconditions;
using tModloaderDiscordBot.Services;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Modules
{
	[Group("status")]
	public class StatusModule : ConfigModuleBase
	{
		public SiteStatusService StatusService { get; set; }

		protected override void BeforeExecute(CommandInfo command)
		{
			base.BeforeExecute(command);
			StatusService.Initialize(Context.Guild.Id);
		}

		[Command("remove")]
		[Alias("delete", "-d")]
		[HasPermission]
		[Priority(1)]
		public async Task RemoveAsync(params string[] args)
		{
			foreach (string noa in args)
			{
				string name = noa.ToLowerInvariant();
				string address = noa;
				IUserMessage msg = await ReplyAsync("Validating address...");

				if (StatusService.HasName(name))
				{
					Config.SiteStatuses.Remove(Config.SiteStatuses.First(x => x.Name.EqualsIgnoreCase(name)));
					await Config.Update();
					await msg.ModifyAsync(x => x.Content = $"Address for `{name}` was removed.");
					continue;
				}

				SiteStatus.CheckUriPrefix(ref address);

				if (StatusService.HasAddress(address))
				{
					Config.SiteStatuses = Config.SiteStatuses.Where(x => !x.Name.EqualsIgnoreCase(address)).ToList();
					await Config.Update();
					await msg.ModifyAsync(x => x.Content = $"Address `{address}` was removed.");
					continue;
				}

				await msg.ModifyAsync(x => x.Content = $"Address `{address}` not found.");
			}
		}

		[Command("add")]
		[Alias("-a")]
		[HasPermission]
		[Priority(1)]
		public async Task AddAsync(string nameParam, string addrParam)
		{
			IUserMessage msg = await ReplyAsync("Validating address...");

			string name = nameParam.ToLowerInvariant();
			string addr = addrParam; /* addrParam.ToLowerInvariant();*/
			SiteStatus.CheckUriPrefix(ref addr);
			bool isLegit = SiteStatus.IsUriLegit(addr, out Uri uri);

			if (!isLegit)
			{
				await msg.ModifyAsync(x => x.Content = $"Address `{addr}` is not a valid web address.");
				return;
			}

			if (StatusService.HasAddress(uri.AbsoluteUri))
			{
				await msg.ModifyAsync(x => x.Content = $"Address `{addr}` is already present.");
				return;
			}

			if (StatusService.HasAddress(name))
			{
				await msg.ModifyAsync(x => x.Content = $"Address for `{name}` already exists.");
				return;
			}

			Config.SiteStatuses.Add(new SiteStatus { Address = addr, Name = name });
			await Config.Update();
			await StatusService.UpdateForConfig(Config);
			await msg.ModifyAsync(x => x.Content = $"Address `{addr}` was added under name `{name}`.");
		}

		[Command]
		[Priority(-99)]
		public async Task Default([Remainder] string toCheckParam = "")
		{
			RestUserMessage msg = await Context.Channel.SendMessageAsync("Performing status checks...");

			try
			{
				StringBuilder sb = new StringBuilder();

				string toCheck = toCheckParam.ToLowerInvariant();

				if (toCheck.Length > 0)
				{
					// TODO levenhstein dist, closest guess
					if (!StatusService.HasName(toCheck))
					{
						await msg.ModifyAsync(x => x.Content = $"Address for `{toCheck}` was not found");
						return;
					}

					(string cachedResult, string url) cachedResult = StatusService.GetCachedResult(toCheck);
					if (!cachedResult.IsDefault())
					{
						await msg.ModifyAsync(x => x.Content = string.Format("{0} {1} {2}", toCheck + ":",
							                      "`" + cachedResult.cachedResult + "`", "(" + cachedResult.url + ")"));
					}
					else
					{
						await msg.ModifyAsync(x => x.Content = "Something went wrong.");
					}

					return;
				}

				if (Config.SiteStatuses.Count <= 0)
				{
					await msg.ModifyAsync(x => x.Content = "No addresses to check.");
					return;
				}

				foreach (SiteStatus status in StatusService.AllSiteStatuses())
				{
					string editString = string.Format("{0} {1} {2}", status.Name + ":", "`" + status.CachedResult + "`",
						"(" + status.Address + ")");
					sb.AppendLine(editString);
				}

				await msg.ModifyAsync(x => x.Content = sb.ToString());
			}
			catch (Exception)
			{
				// Discard PingExceptions and return false;
				await msg.ModifyAsync(x => x.Content = "Something went wrong when trying to check status.");
			}
		}
	}
}