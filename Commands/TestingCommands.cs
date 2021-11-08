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
            await ctx.Channel.SendMessageAsync("goo goo gaa gaa");
        }

        [Command("8ball")]
        [Description("Responds in appropriate manner")]
        public async Task Magic8Ball(CommandContext ctx, params string[] question)
        {
            string[] answers = new string[] {
                "It is certain",
                "Without a doubt",
                "You may rely on it",
                "Yes, definitely", 
                "It is decidely so",
                "As I see it, yes",
                "Most likely",
                "Yes",
                "Outlook good",
                "Signs point to yes",
                "Reply hazy try again",
                "Better not tell you now",
                "Ask again later",
                "Cannot predict now",
                "Concentrate and ask again",
                "Don't count on it",
                "Outlook not so good",
                "My sources say no",
                "Very doubtful",
                "My relpy is no"
            };

            Random random = new Random();
            int rndIndex = random.Next(0, 19);

            await ctx.Channel.SendMessageAsync(answers[rndIndex]);
        }
    }
}
