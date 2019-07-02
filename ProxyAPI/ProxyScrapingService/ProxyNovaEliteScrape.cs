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
    class ProxyNovaEliteScrape : IBulkProxyScraper
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
                    if (cellText[5].Contains("United States"))
                    {
                        var siteText = cellText[0].Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("document.write('12345678", "").Replace("'.substr(8) + '", "").Replace("'); ", "").Trim();

                        Proxy proxy = new Proxy()
                        {
                            URL = "http://" + siteText + ":" + cellText[1].Trim(),
                            Source = "proxynova.com",
                            Country = cellText[5]
                        };
                        yield return proxy;
                    }
                }
            }
        }

        private string GetScreenToScrape()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://www.proxynova.com/proxy-server-list/elite-proxies/");
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
