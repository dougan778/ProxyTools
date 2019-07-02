using System;
using System.Collections.Generic;
using System.Text;
using ProxyModel;

namespace RequestDistribution
{
    /// <summary>
    /// Interface for a class that will retrieve new proxies from outer sources to be used in the application.
    /// </summary>
    public interface IProxySource
    {
        Proxy GetProxy(IProxyRepository repository);
    }
}
