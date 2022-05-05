using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace RalphsDiscordBot.Core.Services
{
    public interface IUserService
    {
        Task<List<Users>> GetLeaderboard();
        Task<List<LotteryTickets>> GetAllTickets();
        Task<Users> GetUserById(string discordId);
        Task<List<Users>> GetUsersWithTickets();
        Task<List<int>> GetLottoTicketsByUser(string discordId);
        Task<bool> PayUser(string discordId, decimal amount, DateTime dateTime, bool stimulus);
        Task<bool> DepositMoney(string discordId, decimal amount);
        Task<bool> WithdrawMoney(string discordId, decimal amount);
        Task<bool> BuyLottoTicket(string discordId, int ticketNumber);
        Task TakeMoney(string discordId, decimal amount);
        Task GiveMoney(string discordId, decimal amount);
        Task SetCockfightStreak(string discordId, bool won);
        Task ClearLotteryTickets();
        Task<decimal> GetTaxRate(string discordId);
    }

    public class UserService : IUserService
    {
        private readonly DbContextOptions<DiscordDBContext> _options;

        public UserService(DbContextOptions<DiscordDBContext> options)
        {
            _options = options;
        }

        public async Task<List<Users>> GetLeaderboard()
        {
            using var context = new DiscordDBContext(_options);

            var leaderboard = await context.Users.FromSqlRaw("SELECT Id, DiscordId, CashBalance + BankBalance AS CashBalance, BankBalance, LastWorked, CockFightWinStreak, LastStimulus, LotteryTicketCount " +
                                                             "FROM DiscordDBContext.dbo.Users " +
                                                             "WHERE DiscordId != 383717290715250688 AND DiscordId != 811705966184628296" + // omit the casino user
                                                             "ORDER BY CashBalance DESC").ToListAsync().ConfigureAwait(false);

            return leaderboard;
        }

        public async Task<decimal> GetTaxRate(string discordId)
        {
            using var context = new DiscordDBContext(_options);

            var leaderboard = await GetLeaderboard();
            decimal totalMoney = 0m;

            foreach (Users u in leaderboard)
            {
                totalMoney += u.CashBalance;
            }

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            decimal totalUserMoney = user.CashBalance + user.BankBalance;
            decimal wealthPercent =  1 - (totalUserMoney / totalMoney);

            return totalUserMoney > 1000000m ? wealthPercent : 1;
        }

        public async Task<List<LotteryTickets>> GetAllTickets()
        {
            using var context = new DiscordDBContext(_options);

            return await context.LotteryTickets.FromSqlRaw("SELECT * FROM DiscordDBContext.dbo.LotteryTickets").ToListAsync().ConfigureAwait(false);
        }

        public async Task<List<Users>> GetUsersWithTickets()
        {
            using var context = new DiscordDBContext(_options);

            return await context.Users.FromSqlRaw("SELECT * FROM DiscordDBContext.dbo.Users WHERE LotteryTicketCount > 0").ToListAsync().ConfigureAwait(false);
        }

        public async Task ClearLotteryTickets()
        {
            using var context = new DiscordDBContext(_options);

            List<Users> users = await context.Users.FromSqlRaw("SELECT * FROM DiscordDBContext.dbo.Users").ToListAsync().ConfigureAwait(false);
            foreach (var user in users)
            {
                user.LotteryTicketCount = 0;
                context.Update(user);
            }

            await context.Database.ExecuteSqlRawAsync("DELETE FROM DiscordDBContext.dbo.LotteryTickets");
            await context.SaveChangesAsync().ConfigureAwait(false);
            return;
        }

        public async Task<Users> GetUserById(string discordId)
        {
            using var context = new DiscordDBContext(_options);
            
            Users user = await context.Users
                .FirstOrDefaultAsync(x => x.DiscordId == discordId).ConfigureAwait(false);

            if (user != null) { return user; }

            // add user to db if not already added
            user = new Users
            {
                DiscordId = discordId,
                CashBalance = 0.00m,
                BankBalance = 0.00m,
                LastWorked = DateTime.Now.Subtract(TimeSpan.FromMinutes(30)),
                LastStimulus = DateTime.Now.Subtract(TimeSpan.FromHours(24)),
                CockFightWinStreak = 0
            };
            
            await context.AddAsync(user).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return user;
        }

        public async Task<List<int>> GetLottoTicketsByUser(string discordId)
        {
            using var context = new DiscordDBContext(_options);
            List<int> results = new List<int>();
            var tickets = await context.LotteryTickets.FromSqlRaw($"SELECT * FROM DiscordDBContext.dbo.LotteryTickets WHERE DiscordId = {discordId}").ToListAsync().ConfigureAwait(false);

            foreach (var ticket in tickets)
            {
                results.Add(ticket.TicketNumber);
            }

            return results;
        }

        public async Task<bool> BuyLottoTicket(string discordId, int ticketNumber)
        {
            using var context = new DiscordDBContext(_options);
            LotteryTickets ticket = await context.LotteryTickets.FirstOrDefaultAsync(x => x.DiscordId == discordId && x.TicketNumber == ticketNumber).ConfigureAwait(false);
            Users user = await GetUserById(discordId).ConfigureAwait(false);

            // cannot buy if ticket already exists
            if (ticket != null) { return false; }

            ticket = new LotteryTickets
            {
                DiscordId = discordId,
                TicketNumber = ticketNumber
            };

            user.LotteryTicketCount++;
            context.Update(user);
            await context.AddAsync(ticket).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        public async Task SetCockfightStreak(string discordId, bool won)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            if (won)
            {
                user.CockFightWinStreak++;
            } else
            {
                user.CockFightWinStreak = 0;
            }

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return;
        }

        public async Task<bool> PayUser(string discordId, decimal amount, DateTime dateTime, bool stimulus)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            user.CashBalance += amount;
            
            if (stimulus)
            {
                TimeSpan interval = DateTime.Now - user.LastStimulus;
                if (interval < TimeSpan.FromHours(24)) { return false; }
                user.LastStimulus = dateTime;
            } else
            {
                TimeSpan interval = DateTime.Now - user.LastWorked;
                if (interval < TimeSpan.FromMinutes(30)) { return false; }
                user.LastWorked = dateTime;
            }

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        public async Task GiveMoney(string discordId, decimal amount)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            user.CashBalance += amount;

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task TakeMoney(string discordId, decimal amount)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            user.CashBalance -= amount;

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<bool> DepositMoney(string discordId, decimal amount)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            // exclude if deposit is more than cash balance or less than 0
            if (amount > user.CashBalance || amount <= 0)
            {
                return false;
            }

            user.CashBalance -= amount;
            user.BankBalance += amount;

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        public async Task<bool> WithdrawMoney(string discordId, decimal amount)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            // exclude if withdrawal is more than bank balance or less than 0
            if (amount > user.BankBalance || amount <= 0)
            {
                return false;
            }

            user.BankBalance -= amount;
            user.CashBalance += amount;

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

    }
}
