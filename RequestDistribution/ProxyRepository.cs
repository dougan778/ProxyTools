using NHibernate;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;
using NHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using System.Linq.Expressions;
using RequestDistribution.Exceptions;
using System.Configuration;
using log4net;
using NHibernate.Transform;
using System.Collections;
using ProxyModel;

namespace RequestDistribution
{
    public class ProxyRepository : IProxyRepository
    {
        ILog ProxyLog = LogManager.GetLogger("Proxies");
        ILog Log = LogManager.GetLogger("RequestDistribution");
        public const int ICE_TIME_MINUTES = 10;
        public const int MAX_USES = 3;
        private Object _queueLock = new object();
        /// <summary>
        /// Collections of proxies that are presumed to be working and ready for use when needed.
        /// </summary>
        private RandomAccessQueue<Proxy> _availableProxies = new RandomAccessQueue<Proxy>();
        /// <summary>
        /// Collection of proxies that are no longer in use, and won't be used again this session.
        /// </summary>
        private List<Proxy> _discardedProxies = new List<Proxy>();

        public int MaxUses { get; } = MAX_USES;

        /// <summary>
        /// Collection of proxies that are fine, but got used the maximum amount of times, and are waiting to get re-used.
        /// </summary>
        private Dictionary<Proxy, DateTime> _icedProxies = new Dictionary<Proxy, DateTime>();
        
        private ISessionFactory _sessionFactory = null;
        private int _minimumProxyCount;
        private Guid SessionID = Guid.NewGuid();

        public void Discard(Proxy proxy)
        {
            if (proxy.HasFailedThisSession)
            {
                _discardedProxies.Add(proxy);
            }
            else if (proxy.SessionFailures + proxy.SessionSuccesses >= ProxyRepository.MAX_USES)
            {
                Log.Info("Icing Proxy: " + proxy.URL);
                lock (_queueLock)
                {
                    _icedProxies.Add(proxy, DateTime.Now.AddMinutes(ICE_TIME_MINUTES));
                }
            }
            else
            {
                throw new Exception("Unable to determine why a proxy was discarded.");
            }
        }

        public static IProxyRepository GetStandardRepository(string connectionString, int? minProxyCount = null)
        {
            var fluentConfig = Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString))
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<Proxy>());

            var fac = fluentConfig.BuildSessionFactory(); // TODO This is fine while getting started, but at some point I'll want this injected down so it doesn't need to be recreated like this (costly).
            ProxyRepository repository = new ProxyRepository(fac, minProxyCount != null ? minProxyCount.Value : 10);
            return repository;
        }

        public ProxyRepository(ISessionFactory sessionFactory, int minProxyCount = 10)
        {
            _sessionFactory = sessionFactory;
            _minimumProxyCount = minProxyCount;
            // TODO use the minimum proxy count to get some proxies pre-loaded.         
        }

        public void RegisterSiteScore(Proxy proxy, string site, bool success, bool banned = false)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    ProxySiteScore score = session.QueryOver<ProxySiteScore>().Where((ProxySiteScore s) => s.Proxy.ProxyId == proxy.ProxyId && s.Site == site).SingleOrDefault<ProxySiteScore>();
                    if (score == null)
                    {
                        score = new ProxySiteScore() { Proxy = proxy, Site = site };
                    }

                    if (banned)
                    {
                        score.Banned = true;
                    }

                    if (success)
                    {
                        score.RegisterSuccess();
                    }
                    else
                    {
                        score.RegisterFailure();
                        
                    }

                    session.SaveOrUpdate(score);
                    transaction.Commit();

                    // TODO Have to refresh "proxy" here.  It may have had a new site score added and it's not reflected in memory.
                    // This basically means the first session in which a proxy is used for a particular site, sentinel will not
                    // know of its site scores and think they don't exist until it's restarted and queried again with them present.
                }
            }
        }

        public void PersistProxy(Proxy proxy)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                using (ITransaction transaction = session.BeginTransaction())
                {
                    session.SaveOrUpdate(proxy);
                    transaction.Commit();
                }
            }            
        }

        /// <summary>
        /// Checks to see if any of the proxies in the iced collection are ready to be activated.
        /// </summary>
        protected void CheckIcedProxies()
        {
            List<Proxy> toRemove = new List<Proxy>();
            // TODO This should lock something.
            foreach (var item in _icedProxies)
            {
                if (item.Value <= DateTime.Now)
                {
                    ProxyLog.Info("Removing proxy from iced state: " + item.Key.URL);
                    item.Key.ResetState();
                    _availableProxies.Add(item.Key);
                    toRemove.Add(item.Key);
                }
            }
            foreach(var proxy in toRemove)
            {
                _icedProxies.Remove(proxy);
            }
        }

        public IList<Proxy> QueryDatabaseForProxies(ProxyFilter pf, ISession session, ProxyFilter.ProxyFilterQuery.SortModes sortMode)
        {
            var transform = new ProxyFilter.ProxyFilterQuery(pf, this.SessionID, sortMode);
            var results = session.CreateSQLQuery(transform.GetSql())
                           .AddEntity("proxy", typeof(Proxy))
                           .SetResultTransformer(Transformers.DistinctRootEntity)
                           .List<Proxy>();
            return results;
        }

        public Proxy GetProxy(ProxyFilter pf = null)
        {
            lock (_queueLock)
            {
                if (pf != null)
                {
                    ProxyLog.Info("Getting Proxy.  Filter: " + pf.GetFilterLogInfo());
                }
                else
                {
                    ProxyLog.Info("Getting Proxy.  No Filter.");
                }
                CheckIcedProxies();
                Proxy proxy = _availableProxies.FirstOrDefault(p => p.MatchesFilter(pf));

                if (proxy != null)
                {
                    ProxyLog.Info("Proxy Retrieved From Available Proxies: " + proxy.GetLogInfo(pf?.Site));
                }
                else
                {
                    using (ISession session = _sessionFactory.OpenSession())
                    {
                        using (var trans = session.BeginTransaction())
                        {
                            IList<Proxy> results = null;
                            try
                            {
                                var sortMode = pf?.Site != null ? ProxyFilter.ProxyFilterQuery.SortModes.SiteScore : ProxyFilter.ProxyFilterQuery.SortModes.Score;
                                results = QueryDatabaseForProxies(pf, session, sortMode);
                            }
                            catch(Exception ex)
                            {
                                Log.Error("Failed to query database for proxies.", ex);
                                throw new ProxyRepositoryFailureException("A failure occurred while querying proxies", ex);
                            }

                            Proxy newProxy = results.FirstOrDefault();
                           
                            if (newProxy == null)
                            {
                                ProxyLog.Info("No Matching Proxy Available");
                                throw new ProxyRepositoryFailureException("No matching proxies available");
                            }
                            ProxyLog.Info("Proxy Retrieved From Database: " + newProxy.GetLogInfo(pf?.Site));

                            newProxy.LastSession = this.SessionID;
                            newProxy.Repository = this;
                            session.Save(newProxy);
                            trans.Commit();
                            return newProxy;
                        }
                    }
                }
                _availableProxies.Remove(proxy);
                return proxy;
            }
        }
    }
}
