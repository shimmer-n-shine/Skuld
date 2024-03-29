﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Skuld.Bot.Discord.Attributes;
using Skuld.Bot.Discord.Preconditions;
using Skuld.Bot.Extensions;
using Skuld.Core;
using Skuld.Core.Extensions;
using Skuld.Core.Extensions.Formatting;
using Skuld.Core.Utilities;
using Skuld.Models;
using Skuld.Services.Messaging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Skuld.Bot.Commands
{
	[Group, Name("Core")]
	public class CoreModule : ModuleBase<ShardedCommandContext>
	{
		public SkuldConfig Configuration { get; set; }

		[Command("help")]
		[Summary("Gets all commands or a specific command's information")]
		[Usage("help")]
		[Ratelimit(5, 5, Measure.Minutes)]
		public async Task Help([Remainder] string command = null)
		{
			using var Database = new SkuldDbContextFactory().CreateDbContext();

			try
			{
				if (command == null)
				{
					string title = $"Commands of: {Context.Client.CurrentUser} that can be invoked in: ";

					if (Context.IsPrivate)
					{
						title += $"DMS/{Context.User.Username}#{Context.User.Discriminator}";
					}
					else
					{
						title += $"{Context.Guild.Name}/{Context.Channel.Name}";
					}

					var embed =
						new EmbedBuilder()
							.AddAuthor(Context.Client)
							.WithRandomColor()
							.WithDescription($"The prefix of **{(Context.Guild == null ? Context.User.FullName() : Context.Guild.Name)}** is: `{(Context.Guild == null ? Configuration.Prefix : (await Database.InsertOrGetGuildAsync(Context.Guild).ConfigureAwait(false)).Prefix)}`");

					Dictionary<ModuleInfo, string> helpBuilder = new Dictionary<ModuleInfo, string>();

					foreach (var module in SkuldApp.CommandService.Modules)
					{
						var commands = await module.GetExecutableCommandsAsync(Context, SkuldApp.Services).ConfigureAwait(false);

						if (commands.Any())
						{
							if (module.IsSubmodule)
							{
								var topParent = module.GetTopLevelParent();
								if (helpBuilder.ContainsKey(topParent))
								{
									var value = helpBuilder.GetValueOrDefault(topParent);

									List<string> desc = new List<string>();
									foreach (var cmd in commands)
									{
										if (cmd.Module == module)
										{
											desc.Add(cmd.Aliases[0]);
										}
									}

									string description = $"\n\nSubmodule: **{module.Name}**\n`";
									foreach (var str in desc.Distinct())
									{
										description += str + ", ";
									}
									description += "`";

									value += description.ReplaceLast(", ", "");

									helpBuilder.Remove(topParent);

									helpBuilder.Add(topParent, value);
								}
							}
							else
							{
								string desc = "";
								foreach (var cmd in commands)
								{
									if (cmd.Module == module)
									{
										desc += $"{cmd.Aliases[0]}, ";
									}
								}

								string description = "`";
								foreach (var str in desc.Split(' ').Distinct())
								{
									description += str + " ";
								}
								description += "`";

								description = description.ReplaceLast(",  ", "");

								helpBuilder.Add(module, description);
							}
						}
					}

					foreach (var helpSection in helpBuilder)
					{
						if (!string.IsNullOrWhiteSpace(helpSection.Value))
						{
							var section = helpSection.Value.ReplaceLast(",  ", "");
							embed.AddField(helpSection.Key.Name, section);
						}
					}

					if (Context.Guild != null)
					{
						if (await Database.InsertOrGetGuildAsync(Context.Guild).ConfigureAwait(false) != null)
						{
							var commands = Database.CustomCommands.ToList().Where(x => x.GuildId == Context.Guild.Id);
							if (commands.Any())
							{
								string commandsText = "";
								foreach (var cmd in commands)
								{
									commandsText += $"{cmd.Name}, ";
								}
								commandsText = commandsText[0..^2];
								embed.AddField("Custom Commands", $"`{commandsText}`");
							}
						}
					}

					var dmchan = await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);

					await embed.QueueMessageAsync(Context, type: Services.Messaging.Models.MessageType.DMS).ConfigureAwait(false);
				}
				else
				{
					string prefix = Configuration.Prefix;

					if (Context.Guild != null)
					{
						prefix = (await Database.InsertOrGetGuildAsync(Context.Guild).ConfigureAwait(false)).Prefix;

						var ccs = Database.CustomCommands.ToList().Where(x => x.GuildId == Context.Guild.Id);
						if (ccs.Any())
						{
							if (ccs.Any(x => x.Name.ToUpperInvariant() == command.ToUpperInvariant()))
							{
								var comd = ccs.FirstOrDefault(x => x.Name.ToUpperInvariant() == command.ToUpperInvariant());

								StringBuilder desc = new StringBuilder();

								desc.AppendLine("**Summary:**")
									.Append("Custom Command")
									.AppendLine()
									.AppendLine()
									.AppendLine("**Can Execute:**")
									.Append("True")
									.AppendLine()
									.AppendLine()
									.AppendLine("**Usage**")
									.Append(prefix)
									.Append(comd.Name)
									.AppendLine();


								await
									EmbedExtensions
										.FromMessage(Context)
										.WithTitle(comd.Name)
										.WithDescription(desc.ToString())
										.QueueMessageAsync(Context)
									.ConfigureAwait(false);

								return;
							}
						}
					}

					var cmd = await SkuldApp.CommandService.GetCommandHelpAsync(Context, command, prefix).ConfigureAwait(false);
					if (cmd == null)
					{
						await $"Sorry, I couldn't find a command like **{command}**.".QueueMessageAsync(Context).ConfigureAwait(false);
					}
					else
					{
						await cmd.QueueMessageAsync(Context).ConfigureAwait(false);
					}
				}
			}
			catch (Exception ex)
			{
				await EmbedExtensions.FromError(ex.Message, Context).QueueMessageAsync(Context).ConfigureAwait(false);
				Log.Error("CMD-HELP", ex.Message, Context, ex);
			}
		}

		[Command("issue")]
		[Summary("Create or get an issue on Github")]
		[Usage("\"Command x doesn't work\" I tried to run command `x` and it threw the error `error`", "\"Command x doesn't work\"")]
		[Ratelimit(1, 10, Measure.Minutes)]
		[RequireService(typeof(Octokit.GitHubClient))]
		public async Task Issue(string title, [Remainder] string content = null)
		{
			var GitClient = SkuldApp.Services.GetRequiredService<Octokit.GitHubClient>();
			if (!string.IsNullOrEmpty(content))
			{
				using var Database = new SkuldDbContextFactory().CreateDbContext();

				var message = new StringBuilder();
				message.AppendLine(content);
				message.AppendLine();
				message.AppendLine("===METADATA===");
				message.AppendLine($"Submitter: {Context.User.Username}");
				message.AppendLine($"Version: Skuld/{SkuldAppContext.Skuld.Key.Version}");

				var newMessage =
					await
						((ITextChannel)Context.Client.GetChannel(Configuration.IssueChannel))
						.SendMessageAsync(
							"",
							embed: EmbedExtensions.FromMessage("New Issue", $"Issue requires approval, react to approve or deny before posting to github", Context)
										.AddField("Content", message.ToString())
										.AddField("Submitter", $"{Context.User.FullName()} ({Context.User.Id})")
									.Build()
						).ConfigureAwait(false);

				await newMessage.AddReactionAsync(DiscordUtilities.Tick_Emote).ConfigureAwait(false);

				await newMessage.AddReactionAsync(DiscordUtilities.Cross_Emote).ConfigureAwait(false);

				Database.Issues.Add(new Issue
				{
					IssueChannelMessageId = newMessage.Id,
					Body = message.ToString(),
					Title = title,
					SubmitterId = Context.User.Id
				});

				await Database.SaveChangesAsync().ConfigureAwait(false);

				await
					EmbedExtensions.FromMessage(
						"Issue Tracker",
						$"Your issue has been submitted for approval, if it ends up on the [issue tracker]({SkuldAppContext.Skuld.Value}/issues) it has been approved",
						Context)
					.QueueMessageAsync(Context)
					.ConfigureAwait(false);
			}
			else
			{
				if (int.TryParse(title, System.Globalization.NumberStyles.Integer, null, out int number))
				{
					var issue = await GitClient.Issue.Get(Configuration.GithubRepository, number).ConfigureAwait(false);

					if (issue != null)
					{
						var Labels = new StringBuilder();

						foreach (var label in issue.Labels)
						{
							Labels.Append(label.Name);

							if (label != issue.Labels.LastOrDefault())
							{
								Labels.Append(", ");
							}
						}

						await
							EmbedExtensions.FromMessage(Context)
							.WithUrl(issue.HtmlUrl)
							.WithTitle(issue.Title)
							.WithDescription(issue.Body)
							.AddInlineField("Labels", Labels.ToString())
							.AddInlineField("Status", issue.State.StringValue.CapitaliseFirstLetter())
							.WithColor(issue.State.StringValue[0] == 'o' ? Color.Green : Color.Red)
						.QueueMessageAsync(Context).ConfigureAwait(false);
					}
					else
					{
						await
							EmbedExtensions.FromError($"Couldn't find an issue with the number: `{title}`", Context)
						.QueueMessageAsync(Context).ConfigureAwait(false);
					}
				}
				else
				{
					var issues = await GitClient.Issue.GetAllForRepository(Configuration.GithubRepository).ConfigureAwait(false);

					var issue = issues.FirstOrDefault(x => x.Title.ToUpperInvariant() == title.ToUpperInvariant());

					if (issue != null)
					{
						var Labels = new StringBuilder();

						foreach (var label in issue.Labels)
						{
							Labels.Append(label.Name);

							if (label != issue.Labels.LastOrDefault())
							{
								Labels.Append(", ");
							}
						}

						await
							EmbedExtensions.FromMessage(Context)
							.WithUrl(issue.HtmlUrl)
							.WithTitle(issue.Title)
							.WithDescription(issue.Body)
							.AddInlineField("Labels", Labels.ToString())
							.AddInlineField("Status", issue.State.StringValue.CapitaliseFirstLetter())
							.WithColor(issue.State.StringValue[0] == 'o' ? Color.Green : Color.Red)
						.QueueMessageAsync(Context).ConfigureAwait(false);
					}
					else
					{
						await
							EmbedExtensions.FromError($"Couldn't find an issue with the name: `{title}`", Context)
						.QueueMessageAsync(Context).ConfigureAwait(false);
					}
				}
			}
		}
	}
}