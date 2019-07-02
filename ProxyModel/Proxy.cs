using System.Linq;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;

namespace ProxyModel
{
    public class Proxy
    {
        public virtual string GetLogInfo(string site = null)
        {
            string result = $"{URL} Score: {Score} ID: {ProxyId} Sess Succ: {SessionSuccesses} Sess Fail: {SessionFailures} Tot Suc: {TotalSuccesses} Tot Fail: {TotalFailures}";
            if (site != null)
            {
                result += "\n";
                var score = ProxySiteScores.FirstOrDefault(s => s.Site == site);
                if (score == null)
                {
                    result += $"No Score available for this proxy on [{site}]";
                }
                else
                {
                    result += $"Site: {score.Site} Banned: {score.Banned} Score: {score.Score} Streak: {score.Streak}";
                }
            }
            return result;
        }
        public const int MAX_SCORE = 4;
        public virtual int SessionSuccesses { get; set; } 
        public virtual int SessionFailures { get; set; }
        public virtual int TotalSuccesses { get; set; }
        public virtual int TotalFailures { get; set; }
        public virtual DateTime AddedDate { get; set; }
        public virtual string Country { get; set; }
        public virtual int Streak { get; set; }
        /// <summary>
        /// Score is used as a way to ensure that proxies which have performed well in the past
        /// can tolerate more failures before being discarded, compared to a proxy that has barely
        /// been used, which should be discarded quickly.
        /// </summary>
        public virtual int Score { get; set; }
        public virtual string URL { get; set; }
        public virtual string Source { get; set; }
        public virtual long ProxyId { get; set; }
        public virtual Guid LastSession { get; set; }

        public virtual bool HasFailedThisSession { get; set; } // Unmapped.

        protected  IList<ProxySiteScore> _proxySiteScores = new List<ProxySiteScore>();
        public virtual IList<ProxySiteScore> ProxySiteScores
        {
            get { return _proxySiteScores; }
            set { _proxySiteScores = value; }
        }

        private IProxyRepository _repository;
        public virtual IProxyRepository Repository { get { return _repository; } set { _repository = value; } }

        public Proxy() { }

        public Proxy(IProxyRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// This indicates whether or not you should be using this proxy.  If this is false,
        /// that probably means that the proxy has been used a bunch lately and needs some time to cool off.
        /// </summary>
        public virtual bool AvailableForUse
        {
            get
            {
                return this.SessionFailures + this.SessionSuccesses < _repository.MaxUses;
            }
        }

        public virtual void Discard()
        {
            _repository.Discard(this);
        }

        /// <summary>
        /// Resets the amount of successes/failures for this session when a proxy is brought back in to use.
        /// </summary>
        public virtual void ResetState()
        {
            SessionFailures = 0;
            SessionSuccesses = 0;
        }

        public virtual void RegisterFailure()
        {
            SessionFailures++;
            TotalFailures++;
            Score--;
            if (Streak > 0)
            {
                Streak = 0;
            }
            Streak--;
            Persist();
            HasFailedThisSession = true;
        }

        public virtual void RegisterSuccess()
        {
            SessionSuccesses++;
            TotalSuccesses++;
            if (Score < MAX_SCORE)
            {
                Score++;
            }
            if (Streak < 0)
            {
                Streak = 0;
            }
            Streak++;
            Persist();
        }

        protected virtual void Persist()
        {
            this._repository.PersistProxy(this);
        }

        public virtual bool MatchesFilter(ProxyFilter pf)
        {
            if (pf == null)
            {
                return true;
            }
            if (pf.Site != null)
            {
                var score = ProxySiteScores.FirstOrDefault(f => string.Equals(pf.Site, f.Site, StringComparison.OrdinalIgnoreCase));

                if (score != null)
                {
                    if (pf.NotBanned && score.Banned)
                    {
                        return false;
                    }

                    if (pf.MinSiteScore != null && pf.MinSiteScore.Value > score.Score)
                    {
                        return false;
                    }
                }

                if (pf.Score != null && pf.Score > this.Score)
                {
                    return false;
                }
                else
                {
                    // If there are no proxies for the site, then we will consider it to match until we are told differently.
                    return true;
                }
            }
            return true;
        }
    }

    public class ProxyMapping : ClassMap<Proxy>
    {
        public ProxyMapping()
        {
            Id(x => x.ProxyId);
            Map(x => x.URL);
            Map(x => x.TotalSuccesses);
            Map(x => x.TotalFailures);
            Map(x => x.Score);
            Map(x => x.Source);
            Map(x => x.LastSession);
            Map(x => x.AddedDate);
            Map(x => x.Country);
            Map(x => x.Streak);
            HasMany(x => x.ProxySiteScores).KeyColumn("ProxyId");
        }
    }
}
