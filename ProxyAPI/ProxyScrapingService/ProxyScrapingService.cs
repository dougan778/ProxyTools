using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;

namespace ProxyAPI.ProxyScrapingService
{
    public class ProxyScrapingService : BackgroundService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ProxyScrapingService));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = GetConfiguration();
            var factory = config.BuildSessionFactory();
            while (!stoppingToken.IsCancellationRequested)
            {
                List<IBulkProxyScraper> scrapers = new List<IBulkProxyScraper>()
                {
                    new USProxyScrape(),
                    new FreeProxyListDotNetScrape(),
                    //GatherProxyScrape, //TODO probably requires chromedriver.
                    new ProxyNovaEliteScrape(),
                    new ProxyNovaUSScrape(),
                    new GimmeProxyScrape(),
                    new ProxyHidesterScrape(),
                    new NordVPNJSONScrape()
                };

                log.Info("Beginning Proxy Retrieval.");
                using (ISession session = factory.OpenSession())
                {
                    ProxyProcessor processor = new ProxyProcessor(session);
                    foreach (var scraper in scrapers)
                    {
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                log.Info("Scrape: " + scraper.GetType().FullName);
                                var proxies = scraper.GetProxies(stoppingToken);
                                processor.Process(proxies);
                            }
                            catch(Exception ex)
                            {
                                log.Error("Scrape failed: " + scraper.GetType().FullName);
                                log.Error(ex);
                            }
                        }
                    }
                }
                log.Info("Proxy Retrieval Complete");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        public static FluentConfiguration GetConfiguration()
        {
            var connectionString = Configuration.DatabaseConnectionString;

            FluentConfiguration result = Fluently.Configure()
                    .Database(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString))
                    .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ProxyModel.Proxy>());
            return result;
        }
    }
}
