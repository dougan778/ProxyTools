using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProxyModel;

namespace ProxyAPI.ProxyScrapingService
{
    public interface IBulkProxyScraper
    {
        IEnumerable<Proxy> GetProxies(CancellationToken cancellationToken);
    }
}
