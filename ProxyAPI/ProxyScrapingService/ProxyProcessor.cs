using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyModel;

namespace ProxyAPI.ProxyScrapingService
{
    class ProxyProcessor
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ISession _session;
        public ProxyProcessor(ISession Session)
        {
            _session = Session;
        }

        public void Process(IEnumerable<Proxy> proxies)
        {
            int proxiesProcessed = 0;
            int proxiesMatching = 0;
            foreach(Proxy proxy in proxies)
            {
                var matching = _session.QueryOver<Proxy>().Where(p => p.URL == proxy.URL).List().FirstOrDefault();
                if (matching == null)
                {
                    proxiesProcessed++;
                    proxy.AddedDate = DateTime.Now;
                    _session.Save(proxy);
                }
                else
                {
                    proxiesMatching++;
                    // TODO update the added time on existing proxy.
                }
            }
            log.Info($"Proxies Processed: {proxiesProcessed} Matching Proxies Skipped: {proxiesMatching}.");
        }
    }
}
