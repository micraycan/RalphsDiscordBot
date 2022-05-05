using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Database.Models
{
    public class LotteryTickets : Entity
    {
        public string DiscordId { get; set; }
        public int TicketNumber { get; set; }
    }
}
