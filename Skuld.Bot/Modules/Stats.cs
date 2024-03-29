﻿using Booru.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Miki.API.Images;
using Skuld.Bot.Discord.Attributes;
using Skuld.Bot.Discord.Preconditions;
using Skuld.Core;
using Skuld.Core.Extensions;
using Skuld.Core.Utilities;
using Skuld.Services.Bot;
using Skuld.Services.Messaging.Extensions;
using SysEx.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Weeb.net;

namespace Skuld.Bot.Commands
{
	[Group, Name("Stats"), RequireEnabledModule]
	public class StatsModule : ModuleBase<ShardedCommandContext>
	{
		public static readonly KeyValuePair<AssemblyName, GitRepoStruct> SysEx = new KeyValuePair<AssemblyName, GitRepoStruct>(
			Assembly.GetAssembly(typeof(SysExClient)).GetName(),
			new GitRepoStruct("exsersewo", "SysEx.Net"));

		public static readonly KeyValuePair<AssemblyName, GitRepoStruct> Booru = new KeyValuePair<AssemblyName, GitRepoStruct>(
			Assembly.GetAssembly(typeof(DanbooruClient)).GetName(),
			new GitRepoStruct("exsersewo", "Booru.Net"));

		public static readonly KeyValuePair<AssemblyName, GitRepoStruct> Weebsh = new KeyValuePair<AssemblyName, GitRepoStruct>(
			Assembly.GetAssembly(typeof(WeebClient)).GetName(),
			new GitRepoStruct("Daniele122898", "Weeb.net"));

		public static readonly KeyValuePair<AssemblyName, GitRepoStruct> Twitch = new KeyValuePair<AssemblyName, GitRepoStruct>(
			Assembly.GetAssembly(typeof(TwitchLib.Api.TwitchAPI)).GetName(),
			new GitRepoStruct("TwitchLib", "TwitchLib"));

		public static readonly KeyValuePair<AssemblyName, GitRepoStruct> Imghoard = new KeyValuePair<AssemblyName, GitRepoStruct>(
			Assembly.GetAssembly(typeof(ImghoardClient)).GetName(),
			new GitRepoStruct("Mikibot", "dotnet-miki-api"));

		public static readonly KeyValuePair<AssemblyName, GitRepoStruct> Discord = new KeyValuePair<AssemblyName, GitRepoStruct>(
			Assembly.GetAssembly(typeof(DiscordShardedClient)).GetName(),
			new GitRepoStruct("discord-net", "Discord.Net"));

		[Command("ping"), Summary("Print Ping")]
		[Ratelimit(20, 1, Measure.Minutes)]
		public async Task Ping()
			=> (await $"Please Wait!!!".QueueMessageAsync(Context).ConfigureAwait(false)).ThenAfter(async x =>
			{
				var mainOffset = Context.Client.GetShardFor(Context.Guild).Latency;

				if (x is IUserMessage msg)
				{
					var msgOffset = (msg.Timestamp - Context.Message.Timestamp).TotalMilliseconds;

					var ping = msgOffset + mainOffset;

					await msg.ModifyAsync(z =>
					{
						z.Content = $"PONG: {ping:N0}ms";
					}).ConfigureAwait(false);
				}
			}, 200);

		[Command("stats"), Summary("All stats")]
		[Ratelimit(20, 1, Measure.Minutes)]
		public async Task StatsAll()
		{
			var GitClient = SkuldApp.Services.GetService<Octokit.GitHubClient>();

			try
			{
				var currentuser = Context.Client.CurrentUser;
				var avatar = currentuser.GetAvatarUrl() ?? currentuser.GetDefaultAvatarUrl();
				var color = Color.Teal;

				if (!Context.IsPrivate)
					color = Context.Guild.GetUser(currentuser.Id).GetHighestRoleColor(Context.Guild);

				var embed = new EmbedBuilder()
					.WithFooter("Generated")
					.WithAuthor(currentuser.Username, "", SkuldAppContext.Website)
					.WithThumbnailUrl(avatar)
					.WithCurrentTimestamp()
					.WithColor(color);

				string apiversions =
					$"[.Net Core: {RuntimeInformation.FrameworkDescription}](https://github.com/dotnet/core){Environment.NewLine}" +
					$"[Booru.Net: {Booru.Key.Version}]({Booru.Value}){Environment.NewLine}" +
					$"[Discord.Net: {Discord.Key.Version}]({Discord.Value}) {Environment.NewLine}" +
					$"[Imghoard: {Imghoard.Key.Version}]({Imghoard.Value}){Environment.NewLine}" +
					$"[SysEx.Net: {SysEx.Key.Version}]({SysEx.Value}){Environment.NewLine}" +
					$"[Twitch: {Twitch.Key.Version}]({Twitch.Value}){Environment.NewLine}" +
					$"[Weebsh: {Weebsh.Key.Version}]({Weebsh.Value})";

				string botstats = "";
				botstats += $"[Skuld: {SkuldAppContext.Skuld.Key.Version}]({SkuldAppContext.Skuld.Value})\n";
				botstats += $"Uptime: {string.Format("{0:dd}d {0:hh}:{0:mm}", DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime))}\n";
				botstats += $"Ping: {Context.Client.GetShardFor(Context.Guild).Latency}ms\n";
				botstats += $"Guilds: {Context.Client.Guilds.Count}\n";
				botstats += $"Shards: {Context.Client.Shards.Count}\n";
				botstats += $"Commands: {SkuldApp.CommandService.Commands.Count()}\n";

				if (GitClient != null)
				{
					var commits = await GitClient.Repository.Commit.GetAll(SkuldAppContext.Skuld.Value.Owner, SkuldAppContext.Skuld.Value.Repo).ConfigureAwait(false);

					botstats += $"Commits: {commits.Count}\n";
					botstats += $"Most Recent Commit: [`{commits[0].Sha[0..7]}`]({commits[0].HtmlUrl}) {commits[0].Commit.Message}";
				}

				string systemstats =
					"Memory Used: " + SkuldAppContext.MemoryStats.GetMBUsage + "MB\n" +
					"Operating System: " + SkuldAppContext.WindowsVersion;

				embed.AddField("Bot", botstats);
				embed.AddField("APIs", apiversions);
				embed.AddField("System", systemstats);

				await embed.Build().QueueMessageAsync(Context).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Error("Stats-Cmd", ex.Message, Context, ex);
				await EmbedExtensions.FromError(ex.Message, Context).QueueMessageAsync(Context).ConfigureAwait(false);
			}
		}
	}
}