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
            helpEmbed.AddField("!deposit", $"Deposit cash into your bank. Specify amount to deposit after command or leave empty and deposit all available cash");
            helpEmbed.AddField("!cockfight", $"Place your bet and send your chicken off to fight.\nYou have a 50% chance to win. Every win you gain 1% until 70%\nIf no bet specified, bet defaults to $100.");
            helpEmbed.AddField("!gamble", $"Select your bet and roll for that amount,\nlowest pays highest roller the difference.\nMinimum $100 required to play, game starts after 60 seconds");
            helpEmbed.AddField("!stimulus", $"Receive a stimulus to satisfy your gambling addiction every 24 hours.");
            helpEmbed.AddField("!lottery", $"View the current lottery pool, draw date TBD.");
            helpEmbed.AddField("!leaderboard", $"View the leaderboard.");
            helpEmbed.AddField("!ticket", $"Buy lottery ticket, 10 available per person. Pick a number between 1 - 50, leave blank for quickpick");
            helpEmbed.AddField("!slots", $"Play the slot machine, middle row only. Default bet is $100");

            await ctx.Channel.SendMessageAsync(embed: helpEmbed).ConfigureAwait(false);
        }

        [Command("leaderboard")]
        [Description("Check the leaderboard")]
        public async Task Leaderboard(CommandContext ctx)
        {
            var leaderboard = await _userService.GetLeaderboard().ConfigureAwait(false);
            string usernamesString = String.Empty;
            string balanceString = String.Empty;
            int count = 1;

            foreach (Users user in leaderboard)
            {
                var member = await ctx.Guild.GetMemberAsync(ulong.Parse(user.DiscordId));
                usernamesString += $"{count}. **{member.DisplayName}**\n";
                balanceString += $"$ {user.CashBalance:N0}\n";
                count++;
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{ctx.Guild.Name} Leaderboard",
                Color = DiscordColor.Cyan
            };

            embed.AddField("Name", usernamesString, true);
            embed.AddField("Total Balance", balanceString, true);

            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("lottery")]
        [Description("Check the current lottery pool")]
        public async Task Lottery(CommandContext ctx)
        {
            Users casinoUser = await _userService.GetUserById(_casinoUserId).ConfigureAwait(false);
            Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
            
            var lotteryEmbed = new DiscordEmbedBuilder
            {
                Title = "Covid420 Casino Lottery",
                Description = $"Casino taxes are collected and given out in a lottery.\n",
                Color = DiscordColor.Cyan
            };

            if (user.LotteryTicketCount > 0)
            {
                string entryText = String.Empty;

                List<int> currentEntries = await _userService.GetLottoTicketsByUser(user.DiscordId).ConfigureAwait(false);

                foreach (var entry in currentEntries)
                {
                    if (entry.Equals(currentEntries.Last()))
                    {
                        entryText += $"{entry}";
                    } else
                    {
                        entryText += $"{entry}, ";
                    }
                }

                lotteryEmbed.AddField($"Your Tickets ({user.LotteryTicketCount}/10)", $"{entryText}");
            }

            List<Users> ticketHolders = await _userService.GetUsersWithTickets().ConfigureAwait(false);
            int totalTickets = 0;
            foreach (Users ticketHolder in ticketHolders)
            {
                totalTickets += ticketHolder.LotteryTicketCount;
            }

            lotteryEmbed.AddField("Current Lottery Pool", $"${casinoUser.CashBalance:N}", true);
            lotteryEmbed.AddField("Total Tickets Sold", $"{totalTickets}", true);
            lotteryEmbed.AddField("Total Participants", $"{ticketHolders.Count} Players", true);
            lotteryEmbed.WithFooter($"Get tickets with !ticket. 10 available per person. Pick a number between 1 - 50, leave blank for quickpick.\n" +
                                    $"First ticket is free, second ticket is free for Nitro Boosters, subsequent tickets cost $100,000. Ten tickets available per person.");

            await ctx.Channel.SendMessageAsync(embed: lotteryEmbed).ConfigureAwait(false);
        }

        [Command("ticket")]
        [Description("Buy a lottery ticket, numbers range from 1-50")]
        public async Task Ticket(CommandContext ctx, int number = 0)
        {
            if (ctx.Channel.Name == "casino")
            {
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
                // if entered number out of range (1-50), use quickpick
                bool quickPick = !(number > 0 && number <= 50);
                int freeTicketCount = ctx.Member.PremiumSince != null ? 2 : 1;
                int maxTickets = 10;
                decimal ticketPrice = 100000m;
                var embed = new DiscordEmbedBuilder { Color = DiscordColor.Green };
                int quickPickNumber = random.Next(1, 50);
                int ticketNumber = number;

                if (quickPick)
                {
                    if (user.LotteryTicketCount > 0)
                    {
                        List<int> numbers = await _userService.GetLottoTicketsByUser(user.DiscordId).ConfigureAwait(false);
                        var range = Enumerable.Range(1, 50).Where(i => !numbers.Contains(i));
                        int index = new Random().Next(1, 50 - numbers.Count);
                        quickPickNumber = range.ElementAt(index);
                    }

                    ticketNumber = quickPickNumber;
                }

                // regular users get first ticket free, nitro users get first two tickets free
                if (user.LotteryTicketCount < freeTicketCount) { ticketPrice = 0; }

                if (user.LotteryTicketCount < maxTickets)
                {
                    if (user.CashBalance >= ticketPrice)
                    {
                        bool succeeded = await _userService.BuyLottoTicket(user.DiscordId, ticketNumber).ConfigureAwait(false);

                        if (succeeded)
                        {
                            embed.Description = $"{ctx.Member.Mention}, you receive ticket {ticketNumber}";
                            await _userService.TakeMoney(user.DiscordId, ticketPrice);
                            embed.AddField("Ticket Price", $"${ticketPrice:N}", true);
                            embed.AddField("Cash Available", $"${(user.CashBalance - ticketPrice):N}", true);
                            embed.AddField("Current Entries", $"{user.LotteryTicketCount + 1}", true);
                        }
                        else
                        {
                            embed.Description = $"{ctx.Member.Mention}, you have already picked {ticketNumber}, please choose another number between 1 and 50.";
                            embed.Color = DiscordColor.Red;
                        }
                    }
                    else
                    {
                        embed.Description = $"{ctx.Member.Mention}, you do not have enough cash available to purchase a ticket.";
                        embed.Color = DiscordColor.Red;
                        embed.AddField("Ticket Price", $"${ticketPrice:N}", true);
                        embed.AddField("Cash Available", $"${user.CashBalance:N}", true);
                    }
                } else
                {
                    embed.Description = $"{ctx.Member.Mention}, you have reached the max amount of allowed tickets.";
                    embed.Color = DiscordColor.Red;
                }

                embed.WithFooter($"First ticket is free, second ticket is free for Nitro Boosters, subsequent tickets cost $100,000. Ten tickets available per person.");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            } else
            {
                await WrongChannelAlert(ctx).ConfigureAwait(false);
            }
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
        [RequireRoles(RoleCheckMode.Any, "Supreme Reader")]
        [Hidden()]
        public async Task TestF(CommandContext ctx)
        {
            /*
            FormulaOneOdds f1Odds = new FormulaOneOdds();

            foreach (string odds in f1Odds.GetF1Odds())
            {
                await ctx.Channel.SendMessageAsync(odds).ConfigureAwait(false);
            }
            */
        }

        [Command("slots")]
        [Description("Slot Machine (middle line only). Tax collected on winnnings for lottery.")]
        public async Task Slots(CommandContext ctx, decimal amount = 100)
        {
            if (ctx.Channel.Name == "casino")
            {
                Users user = await _userService.GetUserById(ctx.Member.Id.ToString()).ConfigureAwait(false);
                DiscordEmoji indicator = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                bool slotsWin = false;
                string resultString = "";
                decimal casinoTax = ctx.Member.PremiumSince != null ? 0.05m : 0.10m;
                decimal userTaxRate = await _userService.GetTaxRate(user.DiscordId).ConfigureAwait(false);
                decimal betAmount = amount;
                decimal winModifier = 10m;
                decimal winAmount = betAmount - (betAmount * (casinoTax * userTaxRate));
                decimal newCashBalance;
                NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                nfi.PercentDecimalDigits = 0;

                List<DiscordEmoji> slotChoices = new List<DiscordEmoji>()
                {
                    DiscordEmoji.FromName(ctx.Client, ":slot_seven:"),
                    DiscordEmoji.FromName(ctx.Client, ":slot_horseshoe:"),
                    DiscordEmoji.FromName(ctx.Client, ":slot_diamond:")
                };

                List<DiscordEmoji> slotResults = new List<DiscordEmoji>();

                // does user have enough money
                if (user.CashBalance >= betAmount)
                {
                    // generate random slots
                    for (int i = 0; i < 9; i++)
                    {
                        slotResults.Add(slotChoices[random.Next(0, slotChoices.Count)]);
                    }

                    // check for middle row win
                    if (slotResults[3] == slotResults[4] && slotResults[4] == slotResults[5])
                    {
                        newCashBalance = user.CashBalance + winAmount * winModifier;
                        slotsWin = true;
                        resultString = "You win";
                        betAmount = winAmount * winModifier;
                        await _userService.GiveMoney(user.DiscordId, betAmount).ConfigureAwait(false);
                    }
                    else // lose
                    {
                        newCashBalance = user.CashBalance - betAmount;
                        slotsWin = false;
                        resultString = "You lose";
                        await _userService.TakeMoney(user.DiscordId, betAmount).ConfigureAwait(false);
                    }

                    // embed builder
                    var slotEmbed = new DiscordEmbedBuilder
                    {
                        Title = ctx.Member.DisplayName,
                        Description = $"{resultString} ${betAmount:N}\n\n" +
                                      $"{slotResults[0]} | {slotResults[1]} | {slotResults[2]}\n" +
                                      $"{slotResults[3]} | {slotResults[4]} | {slotResults[5]} {indicator}\n" +
                                      $"{slotResults[6]} | {slotResults[7]} | {slotResults[8]}",
                        Color = slotsWin ? DiscordColor.Green : DiscordColor.Red,
                    };

                    slotEmbed.WithFooter($"New Cash Balance: ${newCashBalance:N} \nCasino collects {(casinoTax * userTaxRate).ToString("P", nfi)} tax on winnings for the lottery ");

                    await ctx.Channel.SendMessageAsync(embed: slotEmbed).ConfigureAwait(false);
                } 
                else // not enough money
                {
                    await ctx.Channel.SendMessageAsync(embed: new DiscordEmbedBuilder { Description = $"{ctx.Member.Mention}, you don't have enough money.", Color = DiscordColor.Red });
                }
            } 
            else
            {
                await WrongChannelAlert(ctx);
            }
        }

        [Command("draw")]
        [Description("Draw for lottery")]
        [RequireRoles(RoleCheckMode.Any, "Supreme Reader")]
        [Hidden()]
        public async Task Draw(CommandContext ctx)
        {
            int winningTicket = random.Next(1, 50);
            List<LotteryTickets> lotteryTickets = await _userService.GetAllTickets().ConfigureAwait(false);
            Users casino = await _userService.GetUserById(_casinoUserId).ConfigureAwait(false);
            List<Users> allUsers = await _userService.GetUsersWithTickets().ConfigureAwait(false);
            List<Users> winners = new List<Users>();
            string winnerString = String.Empty;

            foreach (LotteryTickets ticket in lotteryTickets)
            {
                if (ticket.TicketNumber == winningTicket) { winners.Add(allUsers.Find(x => x.DiscordId == ticket.DiscordId)); }
            }

            decimal prizeMoney = winners.Count > 0 ? casino.CashBalance / winners.Count : casino.CashBalance;

            var drawingEmbed = new DiscordEmbedBuilder
            {
                Title = $"Lottery results for {DateTime.Now:d}"
            };

            if (winners.Count > 0)
            {
                drawingEmbed.Description = $"Winning tickets sold: {winners.Count}\nTotal Pot: ${casino.CashBalance:N}";

                foreach (Users user in winners)
                {
                    ulong id;
                    if (UInt64.TryParse(user.DiscordId, out id))
                    {
                        DiscordMember winner = await ctx.Guild.GetMemberAsync(id);

                        if (user.Equals(winners.Last())) { winnerString += $"{winner.Mention} ${prizeMoney:N}"; }
                        else { winnerString += $"{winner.Mention} ${prizeMoney:N}\n"; }
                    }

                    await _userService.GiveMoney(user.DiscordId, prizeMoney);
                }
                
                drawingEmbed.AddField("Winners", $"{winnerString}", true);
                drawingEmbed.Color = DiscordColor.Green;

                await _userService.TakeMoney(casino.DiscordId, casino.CashBalance);
                await _userService.ClearLotteryTickets().ConfigureAwait(false);
            } else
            {
                drawingEmbed.Description = "No winning tickets were sold this time.";
                drawingEmbed.Color = DiscordColor.Red;
            }

            
            drawingEmbed.AddField("Winning Number", $"{winningTicket}");
            var msg = await ctx.Channel.SendMessageAsync(embed: drawingEmbed).ConfigureAwait(false);

            await msg.PinAsync();
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
                // double nitroBonus = ctx.Member.PremiumSince != null ? 0.05 : 0;
                double playerStreak = ((double)user.CockFightWinStreak) / 100;
                double streakBonus = playerStreak < 0.20 ? playerStreak : 0.20;
                var fightResult = random.NextDouble();
                double totalStreak = winChance + streakBonus; // + nitroBonus;
                bool fightWon = totalStreak >= fightResult;
                decimal casinoTaxRate = ctx.Member.PremiumSince != null ? 0.05m : 0.10m;
                decimal userTaxRate = await _userService.GetTaxRate(user.DiscordId).ConfigureAwait(false);
                decimal casinoTax = betAmount * (casinoTaxRate * userTaxRate);
                NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                nfi.PercentDecimalDigits = 0;

                // check if user has enough money to bet
                if (user.CashBalance >= betAmount)
                {
                    // check fight results
                    if (fightWon) // win fight
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"{ctx.Member.Mention}, your chicken kicked ass. You win ${betAmount - casinoTax:N}" };
                        embed.Color = DiscordColor.Green;
                        embed.WithFooter($"Current win chance: {totalStreak.ToString("P", nfi)}. New Cash Balance: ${(user.CashBalance + betAmount - casinoTax):N}\nCasino collects {(casinoTaxRate * userTaxRate).ToString("P", nfi)} tax on winnings for the lottery.");
                        await _userService.GiveMoney(ctx.Member.Id.ToString(), betAmount - casinoTax);
                        await _userService.GiveMoney(_casinoUserId, casinoTax); // lottery pool gets 10% of winnings
                        await ctx.Channel.SendMessageAsync(embed: embed);
                    } else // lose fight
                    {
                        DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"{ctx.Member.Mention}, your chicken died. You lose ${betAmount:N}" };
                        embed.Color = DiscordColor.Red;
                        embed.WithFooter($"Current win chance: {totalStreak.ToString("P", nfi)}. New Cash Balance: ${(user.CashBalance - betAmount):N}");
                        await _userService.TakeMoney(ctx.Member.Id.ToString(), betAmount);
                        
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
