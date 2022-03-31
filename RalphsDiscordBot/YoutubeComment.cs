using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RalphsDiscordBot
{
    class YoutubeComment
    {
        public async Task<string> GetVideoCommentAsync(string videoURL, string APIKEY, string APIURL, string VIDEOAPIURL)
        {
            string videoID = Regex.Match(videoURL, @"^.*(youtu\.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]{11}).*").Groups[2].Value;
            string commentRq = APIURL + "&videoId=" + videoID + APIKEY;
            string channelRq = VIDEOAPIURL + "&id=" + videoID + APIKEY;

            List<string> comments = new List<string>();

            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage commentRs = await client.GetAsync(commentRq);
                HttpResponseMessage channelRs = await client.GetAsync(channelRq);
                commentRs.EnsureSuccessStatusCode();
                channelRs.EnsureSuccessStatusCode();
                string responseBodyComments = await commentRs.Content.ReadAsStringAsync();
                string responseBodyChannel = await channelRs.Content.ReadAsStringAsync();

                dynamic commentjson = JsonConvert.DeserializeObject(responseBodyComments);
                dynamic channeljson = JsonConvert.DeserializeObject(responseBodyChannel);

                string channelAuthor = channeljson["items"][0]["snippet"]["channelTitle"];

                if (commentjson["items"].Count > 0)
                {
                    foreach (dynamic item in commentjson["items"])
                    {
                        string comment = item["snippet"]["topLevelComment"]["snippet"]["textOriginal"].ToString();

                        // skip author comments and long comments
                        if (item["snippet"]["topLevelComment"]["snippet"]["authorDisplayName"] != channelAuthor &&
                            comment.Length < 200)
                        {
                            comments.Add(comment);
                        }
                    }
                }
                else // if no comments on video
                {
                    comments.Add("boring");
                }
            } 
            catch
            {
                // if comments are disabled or some other stupid reason google returns an error, boye doesn't like that
                comments.Add("i hate this");
            }

            // choose a random comment
            Random r = new Random();
            int rInt = r.Next(0, comments.Count - 1);

            return comments[rInt];
        }
    }
}
