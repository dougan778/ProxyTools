using NHibernate.Transform;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyModel
{
    public class ProxyFilter
    {
        public ProxyFilter()
        {
            NotCurrentSession = true;
        }

        public string GetFilterLogInfo()
        {
            StringBuilder result = new StringBuilder();
            if (Site != null)
            {
                result.Append($"[Site: {Site}] ");
            }

            if (MinSiteScore != null)
            {
                result.Append($"[MinSiteScore: {MinSiteScore}] ");
            }

            if (NotBanned)
            {
                result.Append("[Not Banned] ");
            }

            if (Score != null)
            {
                result.Append($"[Score: {Score}] ");
            }

            if (NotCurrentSession)
            {
                result.Append("[NotCurrentSession] ");
            }

            if (MaxDaysStale != null)
            {
                result.Append($"[MaxDaysStale {MaxDaysStale}]");
            }
            return result.ToString();
        }

        public string Site { get; set; }
        public int? MinSiteScore { get; set; }
        public bool NotBanned { get; set; }
        public int? Score { get; set; }
        public bool NotCurrentSession { get; set; }
        public int? MaxDaysStale { get; set; } 

        // TODO this is kind of silly because we actually aren't using it as a transformer right now.  I'm leaving it
        // in case I need to do custom transformation for now but it's probably not necessary.
        public sealed class ProxyFilterQuery : IResultTransformer 
        {

            public enum SortModes
            {
                Score,
                SiteScore,
                Random
            }
            private ProxyFilter _proxyFilter;
            private Guid _sessionGuid;
            private SortModes _sortMode;
            public ProxyFilterQuery(ProxyFilter filter, Guid sessionGuid, SortModes sortMode)
            {
                _proxyFilter = filter;
                _sessionGuid = sessionGuid;
                _sortMode = sortMode;
            }
            
            private IEnumerable GetConditions()
            {
                if (_proxyFilter != null)
                {
                    const string nullProxyPart = " OR proxysitescore.proxysitescoreid IS NULL ";
                    List<string> conditions = new List<string>();
                    if (_proxyFilter.NotCurrentSession)
                    {
                        yield return $"proxy.LastSession != '{_sessionGuid}'";
                    }
                    if (_proxyFilter.Site != null)
                    {
                        yield return $"proxysitescore.Site = '{_proxyFilter.Site}' {nullProxyPart}";
                        if (_proxyFilter.MinSiteScore != null)
                        {
                            yield return $"proxysitescore.Score >= {_proxyFilter.MinSiteScore.Value}  {nullProxyPart}";
                        }
                        if (_proxyFilter.NotBanned)
                        {
                            yield return $"proxysitescore.Banned = 0 {nullProxyPart} ";
                        }
                    }
                    if (_proxyFilter.Score != null)
                    {
                        yield return $"proxy.Score >= {_proxyFilter.Score.Value}";
                    }
                    if (_proxyFilter.MaxDaysStale != null)
                    {
                        yield return $"proxy.addedDate >= GETDATE() - {_proxyFilter.MaxDaysStale.Value} ";
                    }
                }
            }

            public string GetSql()
            {
                string query = "SELECT proxy.* "
                             + " FROM proxy "
                             + " LEFT OUTER JOIN proxysitescore ON proxysitescore.proxyid = proxy.proxyid "
                             + " WHERE proxy.proxyid IN ( "
                             + " SELECT TOP 1 proxy.proxyid FROM proxy "
                             + " LEFT OUTER JOIN proxysitescore ON proxysitescore.proxyid = proxy.proxyid ";
                bool first = true;
                foreach(string condition in GetConditions())
                {
                    if (first)
                    {
                        query += " WHERE ";
                        first = false;
                    }
                    else
                    {
                        query += " AND ";
                    }
                    query += $" ({condition})";
                }
                switch (_sortMode)
                {
                    case SortModes.Score:
                        query += " ORDER BY proxy.score desc, proxy.AddedDate desc";
                        break;
                    case SortModes.SiteScore:
                        if (_proxyFilter == null || _proxyFilter.Site == null)
                        {
                            throw new ArgumentException("Cannot sort by a site score when a site is not specified.");
                        }
                        query += " ORDER BY proxysitescore.score desc, proxy.score desc, proxy.AddedDate desc";
                        break;

                    case SortModes.Random:
                        query += " ORDER BY newid() ";
                        break;
                }
                query += ")";
                return query;
            }
            public IList TransformList(IList collection)
            {
                return collection;
            }

            public object TransformTuple(object[] tuple, string[] aliases)
            {
                throw new NotImplementedException();
            }
        }
    }
}
