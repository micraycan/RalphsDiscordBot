using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RalphsDiscordBot.Handlers.Dialogue.Steps
{
    public class DecimalStep : DialogueStepBase
    {
        private IDialogueStep _nextStep;
        private readonly decimal? _minValue;
        private readonly decimal? _maxValue;
        
        public DecimalStep(string content, IDialogueStep nextStep, decimal? minValue = null, decimal? maxValue = null) : base(content)
        {
            _nextStep = nextStep;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public Action<decimal> OnValidResult { get; set; } = delegate { };

        public override IDialogueStep NextStep => _nextStep;

        public void SetNextStep(IDialogueStep nextStep)
        {
            _nextStep = nextStep;
        }

        public override async Task<bool> ProcessStep(DiscordClient client, DiscordChannel channel, DiscordUser user)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Description = "To stop transaction, type cancel",
            };

            embedBuilder.AddField($"{_content}", $"{user.Mention}");

            if (_minValue.HasValue)
            {
                embedBuilder.AddField("Minimum", $"${_minValue.Value}");
            }

            if (_maxValue.HasValue)
            {
                embedBuilder.AddField("Maximum", $"${_maxValue.Value}");
            }

            var interactivity = client.GetInteractivity();

            while (true)
            {
                var embed = await channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);

                OnMessageAdded(embed);

                var messageResult = await interactivity.WaitForMessageAsync(
                    x => x.ChannelId == channel.Id && x.Author.Id == user.Id).ConfigureAwait(false);

                if (messageResult.Result.Content.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!decimal.TryParse(messageResult.Result.Content, out decimal inputValue))
                {
                    await TryAgain(channel, "Input is not a valid number").ConfigureAwait(false);
                    continue;
                }

                if (_minValue.HasValue)
                {
                    if (inputValue < _minValue.Value)
                    {
                        await TryAgain(channel, "Does not meet minimum requirement").ConfigureAwait(false);
                        continue;
                    }
                }

                if (_maxValue.HasValue)
                {
                    if (inputValue > _maxValue.Value)
                    {
                        await TryAgain(channel, "Exceeds maximum requirement").ConfigureAwait(false);
                        continue;
                    }
                }

                OnValidResult(inputValue);

                return false;
            }
        }
    }
}
