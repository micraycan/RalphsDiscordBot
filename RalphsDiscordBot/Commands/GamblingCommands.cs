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

namespace RalphsDiscordBot.Commands
{
    public class GamblingCommands : BaseCommandModule
    {
        private readonly IUserService _userService;
        private static readonly Random random = new Random();

        public GamblingCommands(IUserService userService)
        {
            _userService = userService;
        }

        [Command("work")]
        public async Task Work(CommandContext ctx)
        {
            decimal paymentAmount = Math.Round((decimal)(100.00 + (random.NextDouble() * (500.00 - 100.00))), 2);
            bool canPay = await _userService.PayUser(ctx.Member.Id.ToString(), paymentAmount, DateTime.Now).ConfigureAwait(false);
            Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

            var workEmbed = new DiscordEmbedBuilder { };

            if (!canPay)
            {
                TimeSpan cooldown = TimeSpan.FromMinutes(10) - (DateTime.Now - user.LastWorked);

                workEmbed.Description = "Labor law requires you take a break";
                workEmbed.AddField("Time Remaining", (cooldown.Minutes > 0 ? cooldown.Minutes + " minutes " : cooldown.Seconds + " seconds"));
            }
            else
            {
                workEmbed.Description = $"You slave away for ${paymentAmount}";
                workEmbed.AddField("Cash", $"${user.CashBalance}", true);
                workEmbed.AddField("Bank", $"${user.BankBalance}", true);
            }

            await ctx.Channel.SendMessageAsync(embed: workEmbed).ConfigureAwait(false);
        }

        [Command("bank")]
        public async Task Bank(CommandContext ctx)
        {
            Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);

            var withdrawStep = new DecimalStep("How much would you like to withdraw?", null, minValue: 0.01m, maxValue: user.BankBalance);
            var depositStep = new DecimalStep("How much would you like to deposit?", null, minValue: 0.01m, maxValue: user.CashBalance);
            
            var firstStep = new ReactionStep("Do you want to begin a transaction?", new Dictionary<DiscordEmoji, ReactionStepData>
            {
                { DiscordEmoji.FromName(ctx.Client, ":atm:"), new ReactionStepData { Content = "Withdraw", NextStep = withdrawStep } },
                { DiscordEmoji.FromName(ctx.Client, ":bank:"), new ReactionStepData { Content = "Deposit", NextStep = depositStep } }
            }, user.CashBalance, user.BankBalance );

            firstStep.OnValidResult += async (result) =>
            {
                if (result == DiscordEmoji.FromName(ctx.Client, ":atm:")  && user.BankBalance <= 0)
                {
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { Description = "Not enough money to process transaction" });
                    firstStep.SetNextStep(firstStep);
                }

                if (result == DiscordEmoji.FromName(ctx.Client, ":bank:") && user.CashBalance <= 0)
                {
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { Description = "Not enough money to process transaction" });
                }
            };

            withdrawStep.OnValidResult += async (result) => { await ProcessWithdrawal(ctx, result).ConfigureAwait(false); };
            depositStep.OnValidResult += async (result) =>  { await ProcessDeposit(ctx, result).ConfigureAwait(false); };

            var inputDialogueHandler = new DialogueHandler(ctx.Client, ctx.Message.Channel, ctx.User, firstStep);

            bool succeeded = await inputDialogueHandler.ProcessDialoge().ConfigureAwait(false);

            if (!succeeded)
            {
                var walletEmbed = new DiscordEmbedBuilder
                {
                    Description = $"{ctx.Member.Mention}'s Wallet"
                };

                user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
                walletEmbed.AddField("Cash", $"${user.CashBalance}", true);
                walletEmbed.AddField("Bank", $"${user.BankBalance}", true);

                await ctx.Channel.SendMessageAsync(embed: walletEmbed).ConfigureAwait(false);
                return;
            }
        }

        [Command("cockfight")]
        public async Task CockFight(CommandContext ctx)
        {
            double winChance = 0.50;
            decimal currentBet = 0m;

            Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
            var secondStep = new DecimalStep("How much will you bet?", null, minValue: 0m, maxValue: user.CashBalance);
            var firstStep = new ReactionStep("Do you want to bet on a chicken fight?", new Dictionary<DiscordEmoji, ReactionStepData>
            {
                { DiscordEmoji.FromName(ctx.Client, ":chicken:"), new ReactionStepData { Content = "Yes", NextStep = secondStep } }
            });

            secondStep.OnValidResult += async (result) =>
            {
                var fightResult = random.NextDouble();

                if (winChance >= fightResult && user.CashBalance >= result)
                {
                    currentBet = result * 2;
                    await _userService.PayUser(ctx.Member.Id.ToString(), currentBet, user.LastWorked);
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { Description = $"Your chicken kicked ass, you won ${result * 2}", Color = DiscordColor.Green });
                } else
                {
                    await _userService.TakeMoney(ctx.Member.Id.ToString(), result);
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { Description = $"Your chicken died, you lost ${result}", Color = DiscordColor.Red });
                }
            };

            var inputDialogueHandler = new DialogueHandler(ctx.Client, ctx.Message.Channel, ctx.User, firstStep);

            bool succeeded = await inputDialogueHandler.ProcessDialoge().ConfigureAwait(false);

            if (!succeeded)
            {
                var embedBuilder = new DiscordEmbedBuilder { Description = "Session ended" };

                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                return;
            }

        }

        private async Task ProcessWithdrawal(CommandContext ctx, decimal amount)
        {
            bool canWithdraw = await _userService.WithdrawMoney(ctx.Member.Id.ToString(), amount);
            var withdrawEmbed = new DiscordEmbedBuilder { };

            if (canWithdraw)
            {
                withdrawEmbed.Description = $"Transaction successful, ${amount} withdrawn";
            }
            else
            {
                withdrawEmbed.Description = $"Transaction failed, enter amount to withdraw from bank";
            }

            Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
            withdrawEmbed.AddField("Cash", $"${user.CashBalance}", true);
            withdrawEmbed.AddField("Bank", $"${user.BankBalance}", true);

            await ctx.Channel.SendMessageAsync(embed: withdrawEmbed).ConfigureAwait(false);
        }

        private async Task ProcessDeposit(CommandContext ctx, decimal amount)
        {
            bool canDeposit = await _userService.DepositMoney(ctx.Member.Id.ToString(), amount);
            var depositEmbed = new DiscordEmbedBuilder { };

            if (canDeposit)
            {
                depositEmbed.Description = $"Transaction successful, ${amount} deposited";
            }
            else
            {
                depositEmbed.Description = $"Transaction failed, enter amount of cash you want to deposit into the bank";
            }

            Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
            depositEmbed.AddField("Cash", $"${user.CashBalance}", true);
            depositEmbed.AddField("Bank", $"${user.BankBalance}", true);

            await ctx.Channel.SendMessageAsync(embed: depositEmbed).ConfigureAwait(false);
        }
    }
}
