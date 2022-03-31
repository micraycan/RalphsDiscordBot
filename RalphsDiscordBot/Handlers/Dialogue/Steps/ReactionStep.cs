using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RalphsDiscordBot.Handlers.Dialogue.Steps
{
    public class ReactionStep : DialogueStepBase
    {
        private readonly Dictionary<DiscordEmoji, ReactionStepData> _options;
        private DiscordEmoji _selectedEmoji;
        private IDialogueStep _nextStep;
        private decimal? _cash;
        private decimal? _bank;

        public ReactionStep(string content, Dictionary<DiscordEmoji, ReactionStepData> options, decimal? cash = null, decimal? bank = null) : base(content)
        {
            _options = options;
            _cash = cash;
            _bank = bank;
        }

        public void SetNextStep(IDialogueStep nextStep)
        {
            _nextStep = nextStep;
        }

        public override IDialogueStep NextStep => _options[_selectedEmoji].NextStep;

        public Action<DiscordEmoji> OnValidResult { get; set; } = delegate { };

        public async override Task<bool> ProcessStep(DiscordClient client, DiscordChannel channel, DiscordUser user)
        {
            var cancelEmoji = DiscordEmoji.FromName(client, ":x:");
            var withdrawEmoji = DiscordEmoji.FromName(client, ":atm:");
            var bankEmoji = DiscordEmoji.FromName(client, ":bank:");

            var embedBuilder = new DiscordEmbedBuilder
            {
                Description = "To cancel, react with the :x: emoji"
            };

            embedBuilder.AddField($"{_content}", $"{user.Mention}");
            
            if (_cash.HasValue && _bank.HasValue)
            {
                embedBuilder.AddField("Cash", $"${_cash}", true);
                embedBuilder.AddField("Bank", $"${_bank}", true);
            }

            var interactivity = client.GetInteractivity();

            while (true)
            {
                var embed = await channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);

                OnMessageAdded(embed);

                foreach(var emoji in _options.Keys)
                {
                    await embed.CreateReactionAsync(emoji).ConfigureAwait(false);
                }

                await embed.CreateReactionAsync(cancelEmoji).ConfigureAwait(false);

                var reactionResult = await interactivity.WaitForReactionAsync(
                    x => _options.ContainsKey(x.Emoji) || x.Emoji == cancelEmoji,
                    embed, user).ConfigureAwait(false);

                if(reactionResult.Result.Emoji == cancelEmoji)
                {
                    return true;
                }

                if((reactionResult.Result.Emoji == withdrawEmoji && _bank <= 0) ||
                   (reactionResult.Result.Emoji == bankEmoji && _cash <= 0))
                {
                    await TryAgain(channel, "Not enough money to complete transaction").ConfigureAwait(false);
                    continue;
                }

                _selectedEmoji = reactionResult.Result.Emoji;

                OnValidResult(_selectedEmoji);

                return false;
            }
        }
    }

    public class ReactionStepData
    {
        public IDialogueStep NextStep { get; set; }
        public string Content { get; set; }
    }
}
