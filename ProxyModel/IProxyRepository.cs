using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyModel
{
    public interface IProxyRepository
    {
        Proxy GetProxy(ProxyFilter filter = null);
        void PersistProxy(Proxy proxy);
        void RegisterSiteScore(Proxy proxy, string site, bool success, bool banned = false);
        void Discard(Proxy proxy);
        int MaxUses { get; }
    }
}
