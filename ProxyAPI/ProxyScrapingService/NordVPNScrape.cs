using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using HtmlAgilityPack;
using ProxyModel;

namespace ProxyAPI.ProxyScrapingService
{
    class NordVPNScrape : IBulkProxyScraper
    {
        public IEnumerable<Proxy> GetProxies(CancellationToken token)
        {
            string html = GetScreenToScrape();
            return ScrapeForProxies(html);
        }

        private IEnumerable<Proxy> ScrapeForProxies(string html)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            var rows = document.DocumentNode.SelectNodes(@"//*/tr"); 
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(@"td")?.ToList();
                
                if (cells != null && cells.Count == 4)
                {
                    var cellText = cells.Select(c => c.InnerText).ToList();
                    
                    if (cellText[0].Contains("United States") && cellText[3].Contains("HTTPS"))
                    {
                        Proxy proxy = new Proxy()
                        {
                            URL = "http://" + cellText[1] + ":" + cellText[2],
                            Source = "https://nordvpn.com/free-proxy-list/"
                        };
                        yield return proxy;
                    }
                }
            }
        }

        private string GetScreenToScrape()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"https://nordvpn.com/free-proxy-list/");
            HttpWebResponse rawResponse = (HttpWebResponse)webRequest.GetResponse();
            if (rawResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Bad Status Code: " + rawResponse.StatusCode);
            }
            Stream dataStream = rawResponse.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }
    }
}
