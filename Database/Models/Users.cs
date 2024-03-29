﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Database.Models
{
    public class Users : Entity
    {
        public string DiscordId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CashBalance { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BankBalance { get; set; }
        public DateTime LastWorked { get; set; }
        public DateTime LastStimulus { get; set; }
        public int CockFightWinStreak { get; set; }
        public int LotteryTicketCount { get; set; }
    }
}
