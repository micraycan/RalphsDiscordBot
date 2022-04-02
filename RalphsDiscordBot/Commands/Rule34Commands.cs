using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RalphsDiscordBot.Commands
{
    class Rule34Commands : BaseCommandModule
    {
        [Command("rule34")]
        [Description("Rule 34 lookup")]
        public async Task Rule34(CommandContext ctx, params string[] searchTag)
        {
            string adjustedSearchTag = "";
            string result = "";

            if (ctx.Channel.Id == 919354981025452062)
            {
                for (int i = 0; i < searchTag.Length; i++)
                {
                    if (searchTag.Length - 1 == i)
                    {
                        adjustedSearchTag += searchTag[i];
                    }
                    else
                    {
                        string updatedWord = searchTag[i] + "+";
                        adjustedSearchTag += updatedWord;
                    }
                }

                Rule34Search rule34 = new Rule34Search();

                result = await rule34.GetSearchResultAsync(adjustedSearchTag);
            } else
            {
                await WrongChannelAlert(ctx);
            }

            

            await ctx.Channel.SendMessageAsync(result);
        }

        private async Task WrongChannelAlert(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Description = $"You must use the {ctx.Guild.GetChannel(919354981025452062).Mention} channel to use this command",
                Color = DiscordColor.Red
            };

            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
        }
    }
}
