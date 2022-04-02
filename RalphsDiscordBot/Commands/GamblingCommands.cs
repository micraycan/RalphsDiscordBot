using Database.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RalphsDiscordBot.Core.Services;
using RalphsDiscordBot.Handlers.Dialogue.Steps;
using RalphsDiscordBot.Handlers.Dialogue;
using DSharpPlus.Interactivity.Extensions;
using System.Linq;
using System.Globalization;

namespace RalphsDiscordBot.Commands
{
    public class GamblingCommands : BaseCommandModule
    {
        private readonly IUserService _userService;
        private static readonly Random random = new Random();
        private readonly string _casinoUserId = "383717290715250688";

        public GamblingCommands(IUserService userService)
        {
            _userService = userService;
        }

        [Command("casinoguide")]
        [Description("Guide for using the casino")]
        public async Task CasinoGuide(CommandContext ctx)
        {
            var helpEmbed = new DiscordEmbedBuilder
            {
                Title = "Covid420 Casino Guide",
                Color = DiscordColor.Cyan,
            };

            helpEmbed.AddField("!work", $"Every 30 minutes you can use the !work command to earn cash.\n*Nitro Boosters earn more.*");
            helpEmbed.AddField("!bank", $"Get your available cash and bank balance.");
            helpEmbed.AddField("!withdraw", $"Withdraw money from bank as cash. Specify amount to withdraw after command or leave empty and withdraw all available funds.");
            helpEmbed.AddField("!deopsit", $"Deposit cash into your bank. Specify amount to deposit after command or leave empty and deposit all available cash");
            helpEmbed.AddField("!cockfight", $"Place your bet and send your chicken off to fight.\nYou have a 50% chance to win. Every win you gain 1% until 70%\nIf no bet specified, bet defaults to $100.\n*Nitro Boosters earn more.*");
            helpEmbed.AddField("!gamble", $"Select your bet and roll for that amount,\nlowest pays highest roller the difference.\nMinimum $100 required to play, game starts after 60 seconds");
            helpEmbed.AddField("!stimulus", $"Receive a stimulus to satisfy your gambling addiction every 24 hours.");
            helpEmbed.AddField("!lottery", $"View the current lottery pool, draw date TBD.");
            helpEmbed.AddField("!leaderboard", $"View the leaderboard.");

            await ctx.Channel.SendMessageAsync(embed: helpEmbed).ConfigureAwait(false);
        }

        [Command("leaderboard")]
        [Description("Check the leaderboard")]
        public async Task Leaderboard(CommandContext ctx)
        {
            var leaderboard = await _userService.GetLeaderboard().ConfigureAwait(false);
            string stringBuilder = "Includes both cash and bank balance.\n\n";
            int count = 1;

            foreach (Users user in leaderboard)
            {
                var member = await ctx.Guild.GetMemberAsync(ulong.Parse(user.DiscordId));
                stringBuilder += $"{count}. **{member.DisplayName}** - ${user.CashBalance:N}\n";
                count++;
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{ctx.Guild.Name} Leaderboard",
                Description = stringBuilder,
                Color = DiscordColor.Cyan
            };

            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("lottery")]
        [Description("Check the current lottery pool")]
        public async Task Lottery(CommandContext ctx)
        {
            Users casinoUser = await _userService.GetUserById(_casinoUserId).ConfigureAwait(false);
            var lotteryEmbed = new DiscordEmbedBuilder
            {
                Title = "Covid420 Casino Lottery",
                Description = $"All cockfight wins for the bot are collected for a lottery.\n" +
                              $"*Lottery drawing date TBD*",
                Color = DiscordColor.Cyan
            };

            lotteryEmbed.AddField("Current Lottery Pool", $"${casinoUser.CashBalance:N}");

            await ctx.Channel.SendMessageAsync(embed: lotteryEmbed).ConfigureAwait(false);
        }

        [Command("work")]
        [Description("Work every 30 minutes to earn money. Nitro Boosters earn more")]
        public async Task Work(CommandContext ctx)
        {
            if (ctx.Channel.Name == "casino")
            {
                double minPayment = 7500;
                double maxPayment = 9000;
                decimal paymentAmount = Math.Round((decimal)(minPayment + (random.NextDouble() * (maxPayment - minPayment))), 2);
                decimal fullPayment = ctx.Member.PremiumSince != null ? paymentAmount * 2 : paymentAmount;
                bool succeeded = await _userService.PayUser(ctx.Member.Id.ToString(), fullPayment , DateTime.Now, false).ConfigureAwait(false);
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

                var workEmbed = new DiscordEmbedBuilder { };

                if (!succeeded)
                {
                    TimeSpan cooldown = TimeSpan.FromMinutes(30) - (DateTime.Now - user.LastWorked);

                    workEmbed.Description = $"{ctx.Member.Mention}, labor law requires you take a break";
                    string cooldownFixed = cooldown.Minutes > 0 ? (cooldown.Minutes == 1 ? cooldown.Minutes + " minute" : cooldown.Minutes + " minutes") : cooldown.Seconds + " seconds";
                    workEmbed.AddField($"{cooldownFixed} remaining", $"Please try again later");
                    workEmbed.Color = DiscordColor.Red;
                }
                else
                {
                    workEmbed.Description = $"{ctx.Member.DisplayName} receives ${fullPayment:N} for working.";
                    workEmbed.AddField("Cash", $"${user.CashBalance:N}", true);
                    workEmbed.AddField("Bank", $"${user.BankBalance:N}", true);
                    workEmbed.Color = DiscordColor.Green;
                }

                await ctx.Channel.SendMessageAsync(embed: workEmbed).ConfigureAwait(false);
            } else
            {
                await WrongChannelAlert(ctx).ConfigureAwait(false);
            }
        }

        [Command("stimulus")]
        [Description("Stimulate the economy with extra income to spend at the casino. Can collect once every 24 hours.")]
        public async Task Stimulus(CommandContext ctx)
        {
            if (ctx.Channel.Name == "casino")
            {
                decimal fullPayment = 50000m;
                bool succeeded = await _userService.PayUser(ctx.Member.Id.ToString(), fullPayment, DateTime.Now, true);
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

                var stimEmbed = new DiscordEmbedBuilder { Title = $"{ctx.Member.DisplayName}'s Daily Stimulus" };

                if (!succeeded)
                {
                    TimeSpan cooldown = TimeSpan.FromHours(24) - (DateTime.Now - user.LastStimulus);
                    stimEmbed.Description = $"{ctx.Member.Mention}, you can only receive one stimulus check every 24 hours.";
                    string cooldownFixed = cooldown.Hours > 0 ?
                        (cooldown.Hours == 1 ? cooldown.Hours + " hour" : cooldown.Hours + " hours") :
                        (cooldown.Minutes > 0 ? (cooldown.Minutes == 1 ? cooldown.Minutes + " minute" : cooldown.Minutes + " minutes") : cooldown.Seconds + " seconds");

                    stimEmbed.AddField($"{cooldownFixed} remaining", $"Please try again later");
                    stimEmbed.Color = DiscordColor.Red;
                }
                else
                {
                    stimEmbed.Description = $"{ctx.Member.DisplayName} receives ${fullPayment:N} to gamble away.\nAvailable again in 24 hours.";
                    stimEmbed.AddField("Cash", $"${user.CashBalance:N}", true);
                    stimEmbed.AddField("Bank", $"${user.BankBalance:N}", true);
                    stimEmbed.Color = DiscordColor.Green;
                }

                await ctx.Channel.SendMessageAsync(embed: stimEmbed).ConfigureAwait(false);
            } else
            {
                await WrongChannelAlert(ctx).ConfigureAwait(false);
            }
        }

        [Command("roll")]
        [Description("Roll any number")]
        public async Task Roll(CommandContext ctx, int maxRoll = 100)
        {
            int result = random.Next(1, maxRoll);

            var rollEmbed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = $"{ctx.Message.Author.Mention}"
            };
            rollEmbed.AddField($"(1 - {maxRoll})", $"{result}");

            await ctx.Channel.SendMessageAsync(embed: rollEmbed).ConfigureAwait(false);
        }

        [Command("testf")]
        [Description("testing command")]
        [RequireRoles(RoleCheckMode.Any, "Admin")]
        [Hidden()]
        public async Task TestF(CommandContext ctx)
        {
            // Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

            

        }

        [Command("deposit")]
        [Description("Deposit cash into your bank. If no amount is specified, all cash will be depositied.")]
        public async Task Deposit(CommandContext ctx, decimal? amount = null)
        {
            if (ctx.Channel.Name == "casino")
            {

                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

                // set deposit amount to total cash balance if blank, else deposit amount
                decimal depositAmount = Decimal.Zero;
                if (!amount.HasValue) { depositAmount = user.CashBalance; }
                else { depositAmount = (decimal)amount; }

                bool succeeded = await _userService.DepositMoney(user.DiscordId, depositAmount);

                var depositEmbed = new DiscordEmbedBuilder
                { Description = $"{ctx.Member.Mention}'s Deposit" };

                if (succeeded)
                {
                    depositEmbed.AddField("Transaction Approved", $"Deposited ${depositAmount:N}");
                    depositEmbed.Color = DiscordColor.Green;
                }
                else
                {
                    depositEmbed.AddField("Transaction Declined", "Please make sure you enter a valid number and\n" +
                                                                    "you have enough cash available to deposit\n" +
                                                                    "*You can use the !deposit command with no amount\n" +
                                                                    "to deposit all cash*");
                    depositEmbed.Color = DiscordColor.Red;
                }
                user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

                depositEmbed.AddField("Cash", $"${user.CashBalance:N}", true);
                depositEmbed.AddField("Bank", $"${user.BankBalance:N}", true);

                await ctx.Channel.SendMessageAsync(embed: depositEmbed).ConfigureAwait(false);
            } else
            {
                await WrongChannelAlert(ctx);
            }
        }

        [Command("withdraw")]
        [Description("Withdraw cash from your bank. If no amount is specified, all cash will be withdrawn.")]
        public async Task Withdraw(CommandContext ctx, decimal? amount = null)
        {
            if (ctx.Channel.Name == "casino")
            {
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

                // set withdrawal amount to total bank balance if blank, else withdrawal amount
                decimal withdrawAmount = Decimal.Zero;
                if (!amount.HasValue) { withdrawAmount = user.BankBalance; }
                else { withdrawAmount = (decimal)amount; }

                bool succeeded = await _userService.WithdrawMoney(user.DiscordId, withdrawAmount);

                var withdrawEmbed = new DiscordEmbedBuilder
                { Description = $"{ctx.Member.Mention}'s Deposit" };

                if (succeeded)
                {
                    withdrawEmbed.AddField("Transaction Approved", $"Withdrawn ${withdrawAmount:N}");
                    withdrawEmbed.Color = DiscordColor.Green;
                }
                else
                {
                    withdrawEmbed.AddField("Transaction Declined", "Please make sure you enter a valid number and\n" +
                                                                    "you have enough cash available to withdraw\n" +
                                                                    "*You can use the !withdraw command with no amount\n" +
                                                                    "to withdraw all money*");
                    withdrawEmbed.Color = DiscordColor.Red;
                }
                user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

                withdrawEmbed.AddField("Cash", $"${user.CashBalance:N}", true);
                withdrawEmbed.AddField("Bank", $"${user.BankBalance:N}", true);

                await ctx.Channel.SendMessageAsync(embed: withdrawEmbed).ConfigureAwait(false);
            }
            else
            {
                await WrongChannelAlert(ctx);
            }
        }

        [Command("gamble")]
        [Description("Select your bet and roll for that amount, lowest pays highest roller the difference. Minimum $100 required to play, game starts after 60 seconds")]
        public async Task Gamble(CommandContext ctx, int amount = 100)
        {
            if (ctx.Channel.Name == "casino")
            {
                DiscordEmoji joinEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                DiscordEmoji[] emojiOptions = new DiscordEmoji[] { joinEmoji };

                var interactivity = ctx.Client.GetInteractivity();
                var options = emojiOptions.Select(x => x.ToString());
                int betAmount = amount;
                if (betAmount < 100) { betAmount = 100; }

                var gambleEmbed = new DiscordEmbedBuilder
                {
                    Title = $"${betAmount:N} Roll Signup",
                    Description = $"Wait for users to join and roll for the amount you bet.\n" +
                                  $"Lowest roll pays highest roll the difference.\n" +
                                  $"If you did not roll the highest/lowest number,\n" +
                                  $"then you do not lose/gain any money.\n" +
                                  $"After 10 seconds the game will begin.\n" +
                                  $"$100 minimum\n\n" +
                                  $"{joinEmoji} to join the game",
                    Color = DiscordColor.Purple
                };

                gambleEmbed.AddField("Current Bet", $"${betAmount:N}");

                var gambleMessage = await ctx.Channel.SendMessageAsync(embed: gambleEmbed).ConfigureAwait(false);

                foreach (var option in emojiOptions)
                {
                    await gambleMessage.CreateReactionAsync(option).ConfigureAwait(false);
                }

                var result = await interactivity.CollectReactionsAsync(gambleMessage, TimeSpan.FromSeconds(10)).ConfigureAwait(false);

                var results = result.Select(x => x.Users);
                Dictionary<DiscordMember, int> rollResults = new Dictionary<DiscordMember, int>();

                var resultEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Results",
                    Color = DiscordColor.Green
                };

                foreach (var players in results)
                {
                    foreach (var player in players)
                    {
                        int roll = random.Next(1, betAmount);
                        var u = await ctx.Guild.GetMemberAsync(player.Id);

                        Users user = await _userService.GetUserById(u.Id.ToString()).ConfigureAwait(false);

                        if (!u.IsBot && user.CashBalance >= betAmount)
                        {
                            resultEmbed.AddField($"{u.DisplayName}", $"{roll}", true);
                            rollResults.Add(u, roll);
                        }
                    }
                }

                if (rollResults.Count < 2)
                {
                    var errorEmbed = new DiscordEmbedBuilder
                    {
                        Description = $"Either not enough players registered or not enough\n" +
                                      $"players have the cash amount required to play.",
                        Color = DiscordColor.Red
                    };

                    await ctx.Channel.SendMessageAsync(embed: errorEmbed).ConfigureAwait(false);

                    return;
                }

                var winner = rollResults.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                var loser = rollResults.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                var jackpot = rollResults[winner] - rollResults[loser];
                resultEmbed.Description = "Players do not need to worry about rolling or transferring money,\n" +
                                          "all transactions and rolls are handled by the bot";
                resultEmbed.AddField($"Congratulations to {winner.DisplayName}!", $"{loser.DisplayName} paid {winner.DisplayName} ${jackpot:N}.");

                await _userService.GiveMoney(winner.Id.ToString(), jackpot);
                await _userService.TakeMoney(loser.Id.ToString(), jackpot);

                await ctx.Channel.SendMessageAsync(embed: resultEmbed).ConfigureAwait(false);
            } else
            {
                await WrongChannelAlert(ctx).ConfigureAwait(false);
            }
        }

        [Command("bank")]
        [Description("Check bank and cash balances")]
        public async Task Bank(CommandContext ctx)
        {
            if (ctx.Channel.Name == "casino")
            {
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
                var bankEmbed = new DiscordEmbedBuilder { Description = $"Use the !deposit and !withdraw commands to complete transactions.\n" +
                                                                        $"Add the amount you wish to deposit/withdraw after the command.\n" +
                                                                        $"Using the command with no amount will deposit/withdraw max available.\n" +
                                                                        $"*(ex: !deposit 1000, !withdraw 5000, !deposit)*", };
                bankEmbed.AddField("User", $"{ctx.Member.Mention}", true);
                bankEmbed.AddField("Cash", $"${user.CashBalance:N}", true);
                bankEmbed.AddField("Bank", $"${user.BankBalance:N}", true);

                await ctx.Channel.SendMessageAsync(embed: bankEmbed).ConfigureAwait(false);

            } else
            {
                await WrongChannelAlert(ctx);
            }
        }

        [Command("cockfight")]
        [Description("Bet on a chicken fight, minimum bet $100")]
        public async Task CockFight(CommandContext ctx, decimal amount = 100m)
        {
            if (ctx.Channel.Name == "casino")
            {
                // set minimum bet amount
                decimal betAmount = amount < 100m ? 100m : amount;

                // default win rate 50%, nitro boosters get 5% bonus
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
                double winChance = 0.50;
                double nitroBonus = ctx.Member.PremiumSince != null ? 0.05 : 0;
                double playerStreak = ((double)user.CockFightWinStreak) / 100;
                double streakBonus = playerStreak < 0.20 ? playerStreak : 0.20;
                var fightResult = random.NextDouble();
                double totalStreak = winChance + streakBonus + nitroBonus;
                bool fightWon = totalStreak >= fightResult;
                NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                nfi.PercentDecimalDigits = 0;

                // check if user has enough money to bet
                if (user.CashBalance >= betAmount)
                {
                    // check fight results
                    if (fightWon) // win fight
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"{ctx.Member.Mention}, your chicken kicked ass. You win ${betAmount:N}" };
                        embed.Color = DiscordColor.Green;
                        embed.WithFooter($"Current win chance: {totalStreak.ToString("P", nfi)}. New Cash Balance: ${(user.CashBalance + betAmount):N}");
                        await _userService.GiveMoney(ctx.Member.Id.ToString(), betAmount);
                        await ctx.Channel.SendMessageAsync(embed: embed);
                    } else // lose fight
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"{ctx.Member.Mention}, your chicken died. You lose ${betAmount:N}" };
                        embed.Color = DiscordColor.Red;
                        embed.WithFooter($"Current win chance: {totalStreak.ToString("P", nfi)}. New Cash Balance: ${(user.CashBalance - betAmount):N}");
                        await _userService.TakeMoney(ctx.Member.Id.ToString(), betAmount);
                        await _userService.GiveMoney(_casinoUserId, betAmount);
                        await ctx.Channel.SendMessageAsync(embed: embed);
                    }

                    // update streak in db
                    await _userService.SetCockfightStreak(ctx.Member.Id.ToString(), fightWon);
                } else // if user is broke
                {
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { Description = $"{ctx.Member.Mention}, you don't have enough money.", Color = DiscordColor.Red });
                }
            } else
            {
                await WrongChannelAlert(ctx);
            }
        }

        private async Task WrongChannelAlert(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder 
            { 
                Description = $"You must use the {ctx.Guild.GetChannel(959076723813666816).Mention} channel to use this command",
                Color = DiscordColor.Red 
            };

            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
        }
    }
}
