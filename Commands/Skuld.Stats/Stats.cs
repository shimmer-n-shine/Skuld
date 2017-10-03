﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Skuld.Tools;
using System.Reflection;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace Skuld.Commands
{
    [Group("Stats"),Name("Stats")]
    public class Stats : ModuleBase
    {
        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;

        [Command("ping", RunMode = RunMode.Async), Summary("Print Ping")]
        public async Task Ping()=>
            await MessageHandler.SendChannel(Context.Channel, "PONG: " + Bot.bot.GetShardFor(Context.Guild).Latency.ToString() + "ms");
        [Command("uptime", RunMode = RunMode.Async), Summary("Current Uptime")]
        public async Task Uptime()=>
            await MessageHandler.SendChannel(Context.Channel, $"Uptime: {string.Format("{0:dd} Days {0:hh} Hours {0:mm} Minutes {0:ss} Seconds", DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime))}");
        [Command("", RunMode = RunMode.Async), Summary("All stats")]
        public async Task AllStats()
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Footer = new EmbedFooterBuilder()
                {
                    Text = "Generated"
                },
                Author = new EmbedAuthorBuilder()
                {
                    IconUrl = (await Context.Guild.GetCurrentUserAsync()).GetAvatarUrl(),
                    Name = (await Context.Guild.GetCurrentUserAsync()).Username
                },

                ThumbnailUrl = (await Context.Guild.GetCurrentUserAsync()).GetAvatarUrl(),
                Timestamp = DateTime.Now,
                Title = "Stats",
                Color = Tools.Tools.RandomColor()
            };
            
            embed.AddField("Version",Assembly.GetEntryAssembly().GetName().Version.ToString(),true);
            embed.AddField("Uptime",string.Format("{0:dd}d {0:hh}:{0:mm}", DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime)),true);
            embed.AddField("Pong",Bot.bot.GetShardFor(Context.Guild).Latency + "ms",true);
            embed.AddField("Guilds", Bot.bot.Guilds.Count().ToString(),true);
            embed.AddField("Shards", Bot.bot.Shards.Count().ToString(),true);

            var currProcess = Process.GetCurrentProcess();
            cpuCounter = new PerformanceCounter("Process", "% Processor Time", currProcess.ProcessName);
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            embed.AddField("Commands", Bot.commands.Commands.Count().ToString(),true);
            embed.AddField("CPU Load", GetCurrentCpuUsage(),true);
            embed.AddField("Memory Used", (currProcess.WorkingSet64 / 1024) / 1024 + "MB",true);

            await MessageHandler.SendChannel(Context.Channel, "", embed);
        }
        [Command("netfw", RunMode = RunMode.Async), Summary(".Net Info")]
        public async Task Netinfo()=>
            await MessageHandler.SendChannel(Context.Channel, $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}");
        [Command("discord", RunMode = RunMode.Async), Summary("Discord Info")]
        public async Task Discnet()=>
            await MessageHandler.SendChannel(Context.Channel, $"Discord.Net Library Version: {DiscordConfig.Version}");
        [Command("system", RunMode = RunMode.Async), Summary("System load")]
        public async Task System()
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Footer = new EmbedFooterBuilder() { Text = "Generated" },
                ThumbnailUrl = (await Context.Guild.GetCurrentUserAsync()).GetAvatarUrl(),
                Timestamp = DateTime.Now,
                Title = "System Load",
                Color = Tools.Tools.RandomColor(),
                Author = new EmbedAuthorBuilder()
                {
                    IconUrl = (await Context.Guild.GetCurrentUserAsync()).GetAvatarUrl(),
                    Name = (await Context.Guild.GetCurrentUserAsync()).Username
                }
            };

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            embed.AddField("CPU Load", GetCurrentCpuUsage(),true);
            embed.AddField("Total Free Ram", GetAvailableRAM(),true); 
            embed.AddField("Uptime", $"{UpTime.Days} day(s) {UpTime.Hours}:{UpTime.Minutes}:{UpTime.Seconds}",true);
            embed.AddField("OS", Environment.OSVersion+" "+RuntimeInformation.OSArchitecture,true);
            await MessageHandler.SendChannel(Context.Channel, "", embed);
        }

        public string GetCurrentCpuUsage()
        {
            cpuCounter.NextValue();
            Thread.Sleep(500);
            return String.Format("{0:0.00}%", cpuCounter.NextValue());
        }

        public string GetAvailableRAM()
        {
            float value = ramCounter.NextValue();
            var totalinbytes = new ComputerInfo().TotalPhysicalMemory;
            double kbtotal = totalinbytes / 1024;
            double mbtotal = kbtotal / 1024;
            double gbtotal = kbtotal / (1024 * 1024);
            return String.Format("{0:0.00} GB / {1} GB\n{2} MB / {3} MB", (value / 1024), Math.Round(gbtotal,2), value, Math.Truncate(mbtotal));
        }
        public TimeSpan UpTime
        {
            get
            {
                using (var uptime = new PerformanceCounter("System", "System Up Time"))
                {
                    uptime.NextValue();
                    return TimeSpan.FromSeconds(uptime.NextValue());
                }
            }
        }
    }
}
