using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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

            for (int i = 0; i < searchTag.Length; i++)
            {
                if (searchTag.Length - 1 == i)
                {
                    adjustedSearchTag += searchTag[i];
                } else
                {
                    string updatedWord = searchTag[i] + "+";
                    adjustedSearchTag += updatedWord;
                }
            }

            Rule34Search rule34 = new Rule34Search();

            string result = await rule34.GetSearchResultAsync(adjustedSearchTag);

            await ctx.Channel.SendMessageAsync(result);
        }
    }
}
