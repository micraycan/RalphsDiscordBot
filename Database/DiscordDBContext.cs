using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Database.Models;

namespace Database
{
    public class DiscordDBContext : DbContext
    {
        public DiscordDBContext(DbContextOptions<DiscordDBContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<LotteryTickets> LotteryTickets { get; set; } 
    }
}
