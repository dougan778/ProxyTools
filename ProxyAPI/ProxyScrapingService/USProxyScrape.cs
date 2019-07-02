using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ProxyModel;

namespace ProxyAPI.ProxyScrapingService
{
    class USProxyScrape : IBulkProxyScraper
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
                if (cells != null && cells.Count == 8)
                {
                    var cellText = cells.Select(c => c.InnerText).ToList();
                    if (cellText[6] == "yes"  // HTTPS
                        && cellText[4] != "transparent")
                    {
                        Proxy proxy = new Proxy()
                        {
                            URL = "http://" + cellText[0] + ":" + cellText[1],
                            Source = "us-proxy.org",
                            Country = "USA"
                        };
                        yield return proxy;
                    }
                }
            }
        }

        private string GetScreenToScrape()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://www.us-proxy.org");
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
