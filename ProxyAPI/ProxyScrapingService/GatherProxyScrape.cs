using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ProxyModel;
using HtmlAgilityPack;
using System.Threading;

namespace ProxyAPI.ProxyScrapingService
{
    class GatherProxyScrape : IBulkProxyScraper
    {
        public IEnumerable<ProxyModel.Proxy> GetProxies(CancellationToken cancellationToken)
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
                if (cells != null && cells.Count == 8)
                {
                    var cellText = cells.Select(c => c.InnerText).ToList();
                    if (true) //cellText[4] == "United States")
                    {
                        Proxy proxy = new Proxy()
                        {
                            URL = "http://" + cellText[0] + ":" + cellText[1],
                            Source = "GatherProxy " + cellText[4]
                        };
                        yield return proxy;
                    }
                }
            }
        }

        private string GetScreenToScrape()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://www.gatherproxy.com/proxylist/anonymity/?t=Elite#3");
            webRequest.UserAgent = "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36";
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
