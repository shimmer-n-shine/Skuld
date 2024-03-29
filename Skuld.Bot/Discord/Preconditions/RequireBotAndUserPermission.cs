﻿using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Skuld.Bot.Discord.Preconditions
{
    public class RequireBotAndUserPermission : PreconditionAttribute
    {
        private readonly GuildPermission Permission;

        public RequireBotAndUserPermission(GuildPermission perm)
        {
            Permission = perm;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild == null) return Task.FromResult(PreconditionResult.FromSuccess());

            var perm = GetPermission(context);
            if (perm)
            { return Task.FromResult(PreconditionResult.FromSuccess()); }
            else
            { return Task.FromResult(PreconditionResult.FromError("Either you or I don't have permissions to perform this command")); }
        }

        public bool GetPermission(ICommandContext c)
        {
            if (c.User.IsBot)
            { return false; }

            IGuildUser user = (IGuildUser)c.User;
            IGuildUser botu = c.Guild.GetCurrentUserAsync().Result;

            foreach (var perm in user.GuildPermissions.ToList())
            {
                foreach (var bperm in botu.GuildPermissions.ToList())
                {
                    if (perm == Permission && bperm == Permission)
                        return true;
                }
            }

            return false;
        }
    }
}