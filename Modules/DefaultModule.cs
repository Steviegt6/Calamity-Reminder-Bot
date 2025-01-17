﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using tModloaderDiscordBot.Services;
using tModloaderDiscordBot.Utils;

namespace tModloaderDiscordBot.Modules
{
	[Name("default")]
	public class DefaultModule : BotModuleBase
	{
		// Current classes documented on the Wiki
		private static readonly string[] vanillaClasses = { "item", "projectile", "tile", "npc" };

		private static readonly Dictionary<string, HashSet<string>> vanillaFields =
			new Dictionary<string, HashSet<string>>();

		//public ModService ModService { get; set; }

		//public BaseModule(CommandService commandService, GuildConfigService guildConfigService) : base(commandService, guildConfigService)
		//{
		//}

		[Command("ping")]
		[Summary("Returns the bot response time")]
		[Remarks("ping")]
		public async Task Ping([Remainder] string _ = null)
		{
			string GetDeltaString(long elapsedTime, int latency) =>
				$"\nMessage response time: `{elapsedTime} ms`" +
				$"\nDelta: `{Math.Abs(elapsedTime - latency)} ms`";

			int clientLatency = Context.Client.Latency;
			string baseString = $"Latency: `{clientLatency} ms`";

			IUserMessage msg = await ReplyAsync(baseString);

			Stopwatch sw = Stopwatch.StartNew();

			await msg.ModifyAsync(p => p.Content =
				                      baseString +
				                      "\nMessage response time:" +
				                      "\nDelta:");

			sw.Stop();
			long elapsed = sw.ElapsedMilliseconds;
			await msg.ModifyAsync(x => x.Content =
				                      baseString +
				                      GetDeltaString(elapsed, clientLatency));
		}

		/*[Command("widget")]
		[Alias("widgetimg", "widgetimage")]
		[Summary("Generates a widget image of specified mod")]
		[Remarks("widget <mod>\nwidget examplemod")]
		public async Task Widget([Remainder] string mod)
		{
			mod = mod.RemoveWhitespace();
			(bool result, string _) = await ShowSimilarMods(mod);

			if (result)
			{
				string modFound = ModService.Mods.FirstOrDefault(x => x.EqualsIgnoreCase(mod));

				if (modFound != null)
				{
					IUserMessage msg = await ReplyAsync($"Generating widget for {modFound}...");

					// need perfect string.

					using (HttpClient client = new HttpClient())
					{
						byte[] response = await client.GetByteArrayAsync($"{ModService.WidgetUrl}{modFound}");
						using (MemoryStream stream = new MemoryStream(response))
						{
							await Context.Channel.SendFileAsync(stream, $"widget-{modFound}.png");
						}
					}

					await msg.DeleteAsync();
				}
			}
		}*/

		[Command("wikis")]
		[Alias("ws")]
		[Summary("Generates a search for a term in tModLoader wiki")]
		[Remarks("wikis <search term>\nwikis TagCompound")]
		public async Task WikiSearch([Remainder] string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			string encoded = WebUtility.UrlEncode(searchTerm);
			await ReplyAsync(
				$"tModLoader Wiki results for {searchTerm}: <https://github.com/tModLoader/tModLoader/search?q={encoded}&type=Wikis>");
		}

		[Command("examplemod")]
		[Alias("em", "example")]
		[Summary("Generates a search for a term in ExampleMod source code")]
		[Remarks("examplemod <search term>\nexamplemod OnEnterWorld")]
		public async Task ExampleModSearch([Remainder] string searchTerm)
		{
			searchTerm = searchTerm.Trim();
			string encoded = WebUtility.UrlEncode(searchTerm);
			await ReplyAsync(
				$"ExampleMod results for {searchTerm}: <https://github.com/tModLoader/tModLoader/search?utf8=✓&q={encoded}+path:ExampleMod&type=Code>");
		}

		[Command("ranksbysteamid")]
		[Alias("ranksbyauthor", "listmods")]
		[Summary("Generates a link for the ranksbysteamid of the steamid64 provided.")]
		[Remarks("ranksbysteamid <steam64id>\ranksbysteamid 76561198422040054")]
		public async Task RanksBySteamID([Remainder] string steamid64)
		{
			steamid64 = steamid64.Trim();
			if (steamid64.Length == 17 && steamid64.All(c => c >= '0' && c <= '9'))
			{
				string encoded = WebUtility.UrlEncode(steamid64);
				await ReplyAsync(
					$"tModLoader ranks by steamid results for {steamid64}: <http://javid.ddns.net/tModLoader/tools/ranksbysteamid.php?steamid64={encoded}>");
			}
			else
			{
				await ReplyAsync($"\"{steamid64}\" is not a valid steamid64");
			}

			// Todo: allow users to register their username under a steamid64 and allow username to be used here.
		}

		[Command("documentation")]
		[Alias("doc", "docs")]
		[Summary("Generates a link to tModLoader or Terraria class documentation")]
		[Remarks("doc <classname>[.<field/method name>]\ndoc Item.value")]
		public async Task Documentation([Remainder] string searchTerm)
		{
			// TODO: use XML file to show inline documentation.
			string[] parts = searchTerm.Split(' ', '.');
			string className = parts[0].Trim();
			string classNameLower = className.ToLowerInvariant();
			string methodName = parts.Length >= 2 ? parts[1].Trim().ToLowerInvariant() : "";
			string methodNameLower = methodName.ToLowerInvariant();

			if (vanillaClasses.Contains(classNameLower))
			{
				if (methodName == "")
				{
					await ReplyAsync(
						$"Documentation for `{className}`: <https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation>");
				}
				else
				{
					if (!vanillaFields.TryGetValue(classNameLower, out HashSet<string> fields))
					{
						fields = new HashSet<string>();
						//using (var client = new WebClient())
						//{
						//string response = await client.DownloadStringTaskAsync($"https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation");
						HtmlWeb hw = new HtmlWeb();
						HtmlDocument doc = await hw.LoadFromWebAsync(
							$"https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation");
						foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
						{
							HtmlAttribute att = link.Attributes["href"];
							if (att.Value.StartsWith("#") && att.Value.Length > 1)
							{
								fields.Add(att.Value.Substring(1));
							}
						}
						//}

						vanillaFields[classNameLower] = fields;
					}

					if (fields.Contains(methodNameLower))
					{
						await ReplyAsync(
							$"Documentation for `{className}.{methodName}`: <https://github.com/tModLoader/tModLoader/wiki/{className}-Class-Documentation#{methodNameLower}>");
					}
					else
					{
						await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
					}
				}
			}
			else
			{
				// might be a modded class:
				//http://tmodloader.github.io/tModLoader/html/namespace_terraria_1_1_mod_loader.js

				using (WebClient client = new WebClient())
				{
					string response = await client.DownloadStringTaskAsync(
						"http://tmodloader.github.io/tModLoader/html/namespace_terraria_1_1_mod_loader.js");
					response = string.Join("\n", response.Split("\n").Skip(1)).TrimEnd(';');
					List<List<object>> resultObject = JsonConvert.DeserializeObject<List<List<object>>>(response);
					IEnumerable<List<object>> stringResultsOnly = resultObject.Where(x => x.All(y => y is string));
					List<List<string>> result = new List<List<string>>();
					foreach (List<object> item in stringResultsOnly)
						result.Add(new List<string> { item[0] as string, item[1] as string, item[2] as string });
					List<string> r = result.Find(x => x[0].EqualsIgnoreCase(classNameLower));
					if (r != null)
					{
						className = r[0];
						if (methodName == "")
						{
							await ReplyAsync(
								$"Documentation for `{className}`: http://tmodloader.github.io/tModLoader/html/{r[1]}");
						}
						else
						{
							Console.WriteLine("http://tmodloader.github.io/tModLoader/html/{r[2]}.js");
							// now to find method name
							response = await client.DownloadStringTaskAsync(
								$"http://tmodloader.github.io/tModLoader/html/{r[2]}.js");
							response = string.Join("\n", response.Split("\n").Skip(1)).TrimEnd(';');
							result = JsonConvert.DeserializeObject<List<List<string>>>(response);
							r = result.Find(x => x[0].EqualsIgnoreCase(methodNameLower));
							if (r != null)
							{
								methodName = r[0];
								await ReplyAsync(
									$"Documentation for `{className}.{methodName}`: http://tmodloader.github.io/tModLoader/html/{r[1]}");
							}
							else
							{
								await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
							}
						}
					}
					else
					{
						if (methodName == "")
						{
							await ReplyAsync($"Documentation for `{className}` not found");
						}
						else
						{
							await ReplyAsync($"Documentation for `{className}.{methodName}` not found");
						}
					}
				}
			}
		}

		/*[Command("mod")]
		[Alias("modinfo")]
		[Summary("Shows info about a mod")]
		[Remarks("mod <internal modname> --OR-- mod <part of name>\nmod examplemod")]
		[Priority(-99)]
		public async Task Mod([Remainder] string mod)
		{
			mod = mod.RemoveWhitespace();

			if (mod.EqualsIgnoreCase(">count"))
			{
				await ReplyAsync($"Found `{ModService.Mods.Count()}` cached mods");
				return;
			}

			(bool result, string str) = await ShowSimilarMods(mod);

			if (result)
			{
				if (string.IsNullOrEmpty(str))
				{
					// Fixes not finding files
					mod = ModService.Mods.FirstOrDefault(
						m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));
					if (mod == null)
					{
						return;
					}
				}
				else
				{
					mod = str;
				}

				// Some mod is found continue.
				JObject modjson =
					JObject.Parse(await FileUtils.FileReadToEndAsync(new SemaphoreSlim(1, 1), ModService.ModPath(mod)));
				EmbedBuilder eb = new EmbedBuilder()
					.WithTitle("Mod: ")
					.WithCurrentTimestamp()
					.WithAuthor(new EmbedAuthorBuilder
					{
						IconUrl = Context.Message.Author.GetAvatarUrl(),
						Name = $"Requested by {Context.Message.Author.FullName()}"
					});

				foreach (JProperty property in modjson.Properties().Where(x => !string.IsNullOrEmpty(x.Value.ToString())))
				{
					string name = property.Name;
					JToken value = property.Value;

					if (name.EqualsIgnoreCase("displayname"))
					{
						eb.Title += value.ToString();
					}
					else if (name.EqualsIgnoreCase("downloads"))
					{
						eb.AddField("# of Downloads", $"{property.Value:n0}", true);
					}
					else if (name.EqualsIgnoreCase("updatetimestamp"))
					{
						eb.AddField("Last updated",
							DateTime.Parse($"{property.Value}").ToString("dddd, MMMMM d, yyyy h:mm:ss tt",
								new CultureInfo("en-US")), true);
					}
					else if (name.EqualsIgnoreCase("iconurl"))
					{
						eb.ThumbnailUrl = value.ToString();
					}
					else if (name.EqualsIgnoreCase("modloaderversion"))
					{
						eb.AddField("tModLoader Version", value.ToString().Split(" ")[1], true);
					}
					else
					{
						eb.AddField(name.FirstCharToUpper(), value, true);
					}
				}

				eb.AddField("Widget", $"<{ModService.WidgetUrl}{mod}>", true);
				using (HttpClient client = new HttpClient())
				{
					HttpResponseMessage response = await client.GetAsync(ModService.QueryHomepageUrl + mod);
					string postResponse = await response.Content.ReadAsStringAsync();
					if (!string.IsNullOrEmpty(postResponse) && !postResponse.StartsWith("Failed:"))
					{
						eb.Url = postResponse;
						eb.AddField("Homepage", $"<{postResponse}>", true);
					}
				}

				await ReplyAsync("", embed: eb.Build());
			}
		}*/

		// Helper method
		/*private async Task<(bool, string)> ShowSimilarMods(string mod)
		{
			IEnumerable<string> mods = ModService.Mods.Where(m => string.Equals(m, mod, StringComparison.CurrentCultureIgnoreCase));

			if (mods.Any())
			{
				return (true, string.Empty);
			}

			bool cached = await ModService.TryCacheMod(mod);
			if (cached)
			{
				return (true, string.Empty);
			}

			const string msg = "Mod with that name doesn\'t exist";
			string modMsg = "\nNo similar mods found...";

			// Find similar mods

			string[] similarMods =
				ModService.Mods
					.Where(m => m.Contains(mod, StringComparison.CurrentCultureIgnoreCase)
					            && m.LevenshteinDistance(mod) <= m.Length - 2) // prevents insane amount of mods found
					.ToArray();

			if (similarMods.Any())
			{
				if (similarMods.Length == 1)
				{
					return (true, similarMods.First());
				}

				modMsg = "\nDid you possibly mean any of these?\n" + similarMods.PrettyPrint();
				// Make sure message doesn't exceed discord's max msg length
				if (modMsg.Length > 2000)
				{
					modMsg = modMsg.Cap(2000 - msg.Length);
					// Make sure message doesn't end with a half cut modname
					int index = modMsg.LastIndexOf(',');
					string lastModClean = modMsg.Substring(index + 1).Replace("`", "").Trim();
					if (ModService.Mods.All(m => m != lastModClean))
					{
						modMsg = modMsg.Substring(0, index);
					}
				}
			}

			await ReplyAsync($"{msg}{modMsg}");
			return (false, string.Empty);
		}*/
	}
}