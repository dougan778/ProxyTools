using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using ProxyModel;

namespace ProxyAPI.ProxyScrapingService
{
    class ProxyHidesterScrape : IBulkProxyScraper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public IEnumerable<Proxy> GetProxies(CancellationToken token)
        {
            for (int i = 0; i < 5; i++)
            {
                foreach(Proxy proxy in GrabProxiesFromServer(i))
                {
                    yield return proxy;
                }
                
                log.Info("Sleeping 5 seconds in-between Hidester requests.");
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            }
        }

        private IEnumerable<Proxy> GrabProxiesFromServer(int page)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://hidester.com/proxydata/php/data.php?mykey=data&offset=" + page + "&limit=10&orderBy=latest_check&sortOrder=DESC&country=&port=&type=undefined&anonymity=undefined&ping=undefined&gproxy=2");
            webRequest.Referer = "https://hidester.com/proxylist/";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36";
            HttpWebResponse rawResponse = (HttpWebResponse)webRequest.GetResponse();

            if (rawResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Bad Status Code: " + rawResponse.StatusCode);
            }
            Stream dataStream = rawResponse.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            var responseText = reader.ReadToEnd();
            dynamic stuff = JsonConvert.DeserializeObject(responseText);
            dynamic taco = stuff[0];
            dynamic taco2 = stuff[1];
            List<Proxy> result = new List<Proxy>();
            for (int i = 0; i < stuff.Count; i++)
            {
                dynamic listing = stuff[i];
                if (listing.anonymity != "Transparent" )
                {
                    Proxy proxy = new Proxy()
                    {
                        URL = "http://" + listing.IP + ":" + listing.PORT,
                        Source = "Proxy Hidester",
                        Country = listing.country
                    };
                    result.Add(proxy);
                }
            }
            return result;
        }

        private string GetScreenToScrape()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://www.proxynova.com/proxy-server-list/country-us/");
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
