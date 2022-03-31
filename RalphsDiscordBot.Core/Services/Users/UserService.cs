using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace RalphsDiscordBot.Core.Services
{
    public interface IUserService
    {
        Task<Users> GetUserById(string discordId);
        Task<bool> PayUser(string discordId, decimal amount, DateTime dateTime);
        Task<bool> DepositMoney(string discordId, decimal amount);
        Task<bool> WithdrawMoney(string discordId, decimal amount);
        Task TakeMoney(string discordId, decimal amount);
    }

    public class UserService : IUserService
    {
        private readonly DbContextOptions<DiscordDBContext> _options;

        public UserService(DbContextOptions<DiscordDBContext> options)
        {
            _options = options;
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
                LastWorked = DateTime.Now.Subtract(TimeSpan.FromMinutes(10))
            };

            await context.AddAsync(user).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return user;
        }

        public async Task<bool> PayUser(string discordId, decimal amount, DateTime dateTime)
        {
            using var context = new DiscordDBContext(_options);

            Users user = await GetUserById(discordId).ConfigureAwait(false);

            TimeSpan interval = DateTime.Now - user.LastWorked;

            // over limit
            if (interval < TimeSpan.FromMinutes(10))
            {
                return false;
            }

            user.CashBalance += amount;
            user.LastWorked = dateTime;

            context.Update(user);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return true;
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
