using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RalphsDiscordBot
{
    public class FormulaOneOdds
    {

        public List<string> GetF1Odds()
        {
            var url = "https://www.vegasinsider.com/auto-racing/odds/f1/";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants("ul");
            List<string> result = new List<string>();

            foreach (HtmlNode node in nodes)
            {
                result.Add(node.InnerText);
            }

            return result;
        }
    }
}
