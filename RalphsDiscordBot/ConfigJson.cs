﻿using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RalphsDiscordBot
{
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string Prefix { get; private set; }

        [JsonProperty("APIKEY")]
        public string APIKEY { get; private set; }

        [JsonProperty("APIURL")]
        public string APIURL { get; private set; }

        [JsonProperty("VIDEOAPIURL")]
        public string VIDEOAPIURL { get; private set; }
    }
}
