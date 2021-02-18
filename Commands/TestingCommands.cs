using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RalphsDiscordBot.Commands
{
    public class TestingCommands : BaseCommandModule
    {
        [Command("test")]
        [Description("Responds in appropriate manner")]
        public async Task Test(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("goo goo gaa gaa").ConfigureAwait(false);
        }
    }
}
