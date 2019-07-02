using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ProxyModel;

namespace ProxyAPI.ProxyScrapingService
{
    class NordVPNJSONScrape : IBulkProxyScraper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public IEnumerable<Proxy> GetProxies(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 100; i+= 25)
            {
                string json = GetJSON(i);
                foreach(Proxy p in GetProxiesFromJson(json))
                {
                    yield return p;
                }
                log.Info("Sleeping 1 minute before more nordvpn");
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
            }
        }

        private IEnumerable<Proxy> GetProxiesFromJson(string json)
        {
            dynamic stuff = JsonConvert.DeserializeObject(json);
            foreach (dynamic entry in stuff)
            {
                string IP = "";
                string port = "";
                foreach(dynamic property in entry)
                {
                    if (property.Name == "ip")
                    {
                        IP = property.Value;
                    }

                    if (property.Name == "port")
                    {
                        port = property.Value;
                    }
                }

                Proxy proxy = new Proxy()
                {
                    URL = "http://" + IP + ":" + port,
                    Source = "NordVPN JSON",
                    Country = "USA"
                };
                yield return proxy;
            }
        }

        private string GetJSON(int offset)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"https://nordvpn.com/wp-admin/admin-ajax.php?searchParameters%5B0%5D%5Bname%5D=proxy-country&searchParameters%5B0%5D%5Bvalue%5D=united+states&searchParameters%5B1%5D%5Bname%5D=proxy-ports&searchParameters%5B1%5D%5Bvalue%5D=&searchParameters%5B2%5D%5Bname%5D=https&searchParameters%5B2%5D%5Bvalue%5D=on&offset=" + offset + "&limit=25&action=getProxies");
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
