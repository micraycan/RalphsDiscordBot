using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RalphsDiscordBot.Commands
{
    public class GamblingCommands : BaseCommandModule
    {
        [Command("gamble")]
        public async Task Gamble(CommandContext ctx)
        {
            var gambleEmbed = new DiscordEmbedBuilder
            {
                Title = "Test Gamble Embed",
                Color = DiscordColor.HotPink
            };

            await ctx.Channel.SendMessageAsync(embed: gambleEmbed);
        }
    }
}
