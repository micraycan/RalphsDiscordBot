﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace RalphsDiscordBot.Handlers.Dialogue.Steps
{
    public interface IDialogueStep
    {
        Action<DiscordMessage> OnMessageAdded { get; set; }
        IDialogueStep NextStep { get; }
        Task<bool> ProcessStep(DiscordClient client, DiscordChannel channel, DiscordUser user);
    }
}
