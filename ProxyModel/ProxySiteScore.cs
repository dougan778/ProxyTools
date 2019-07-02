using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyModel
{
    /// <summary>
    /// This tracks how effective a given proxy has been at a given site (ex bestbuy.com)
    /// </summary>
    public class ProxySiteScore
    {

        public virtual long ProxySiteScoreId { get; set; }
        public virtual int Successes { get; set; }
        public virtual int Failures { get; set; }
        /// <summary>
        /// Score is used as a way to ensure that proxies which have performed well in the past
        /// can tolerate more failures before being discarded, compared to a proxy that has barely
        /// been used, which should be discarded quickly.
        /// </summary>
        public virtual int Score { get; set; }
        public virtual bool Banned { get; set; }
        public virtual Proxy Proxy { get; set; }
        public virtual int Streak { get; set; }
        /// <summary>
        /// The site that this information is for.  bestbuy.com, walmart.com, etc.
        /// </summary>
        public virtual string Site { get; set; }

        public virtual void RegisterSuccess()
        {
            if (Score < 100)
            {
                Score += 3;
            }
            Successes++;
            if (Streak < 0)
            {
                Streak = 0;
            }
            Streak++;
        }

        public virtual void RegisterFailure()
        {
            Score--;
            Failures++;
            if (Streak > 0)
            {
                Streak = 0;
            }
            Streak--;
        }
    }

    public class ProxySiteScoreMapping : ClassMap<ProxySiteScore>
    {
        public ProxySiteScoreMapping()
        {
            Id(x => x.ProxySiteScoreId);
            Map(x => x.Site);
            Map(x => x.Successes);
            Map(x => x.Failures);
            Map(x => x.Score);
            Map(x => x.Banned);
            Map(x => x.Streak);
            References(x => x.Proxy).Column("ProxyId");
        }
    }
}
