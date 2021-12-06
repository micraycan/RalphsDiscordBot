using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RalphsDiscordBot
{
    class Rule34Search
    {
        public async Task<string> GetSearchResultAsync(string searchTag)
        {
            string searchRq = "https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&limit=100&json=1&tags=" + searchTag;

            HttpClient client = new HttpClient();
            HttpResponseMessage searchRs = await client.GetAsync(searchRq);
            searchRs.EnsureSuccessStatusCode();
            string responseBodySearch = await searchRs.Content.ReadAsStringAsync();

            dynamic searchjson = JsonConvert.DeserializeObject(responseBodySearch);

            List<string> images = new List<string>();

            if (searchjson != null)
            {
                foreach (dynamic result in searchjson)
                {
                    string image = result["file_url"];
                    images.Add(image);
                }
            } else
            {
                images.Add("Couldn't find anything");
            }

            Random r = new Random();
            int rInt = r.Next(0, images.Count - 1);

            return images[rInt];
        }
    }
}
