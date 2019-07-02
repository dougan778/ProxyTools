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
    class GimmeProxyScrape : IBulkProxyScraper
    {
        public IEnumerable<Proxy> GetProxies(CancellationToken cancellationToken)
        {
            // Only get one proxy.  Gimmeproxy is nice but they have a daily limit of 240.
            for (int i = 0; i < 1; i++)
            {
                var url = "http://gimmeproxy.com/api/getProxy?post=true&maxCheckPeriod=3600&protocol=http&supportsHttps=true&api_key=" + Configuration.GimmeProxyAPIKey;
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                WebProxy myProxy = new WebProxy();
                HttpWebResponse rawResponse = null;
                try
                {
                    rawResponse = (HttpWebResponse)webRequest.GetResponse();
                    if (rawResponse.StatusCode != HttpStatusCode.OK)
                    {
                        
                    }
                }
                catch (System.Net.WebException ex)
                {
                }

                Stream dataStream = rawResponse.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();

                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(responseFromServer);

                Proxy proxy = new Proxy();
                proxy.Source = "GimmeProxy ";
                proxy.Country = json.country;
                proxy.URL = json.curl;
                yield return proxy;
            }
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
                            Source = "us-proxy.org"
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
