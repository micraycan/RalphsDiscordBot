using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Net;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RalphsDiscordBot.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RalphsDiscordBot
{
    class Bot
    {
        public DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }

        private string APIKEY;
        private string APIURL;
        private string VIDEOAPIURL;

        public async Task RunAsync()
        {
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            APIKEY = configJson.APIKEY;
            APIURL = configJson.APIURL;
            VIDEOAPIURL = configJson.VIDEOAPIURL;

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.All
            };

            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;
            Client.GuildMemberAdded += MemberAddedHandler;
            Client.MessageCreated += MessageCreatedHandler;

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = true // set up custom one eventually
            };

            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<TestingCommands>();
            Commands.RegisterCommands<GamblingCommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task OnClientReady(object sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task MemberAddedHandler(DiscordClient s, GuildMemberAddEventArgs e)
        {
            // automatically add scrubs role to new users to pacify my OCD
            e.Member.GrantRoleAsync(e.Guild.GetRole(310980088122441728));

            return Task.CompletedTask;
        }

        private async Task MessageCreatedHandler(DiscordClient s, MessageCreateEventArgs e)
        {
            // check for youtube links to get comments
            if (e.Message.Content.StartsWith("https://www.youtube.com") ||
                e.Message.Content.StartsWith("https://youtube.com") || 
                e.Message.Content.StartsWith("https://youtu.be"))
            {
                YoutubeComment ytComment = new YoutubeComment();
                string comment = await ytComment.GetVideoCommentAsync(e.Message.Content, APIKEY, APIURL, VIDEOAPIURL);

                await s.SendMessageAsync(e.Channel, comment);
            }
        }
    }
}
