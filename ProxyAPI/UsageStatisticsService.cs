using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyAPI
{
    public class UsageStatisticsService : BackgroundService
    {
        public class Usage
        {
            public long ProxiesRequested { get; set; }
            public long SuccessesReported { get; set; }
            public long SiteFailuresReported { get; set; }
            public long ProxyFailuresReported { get; set; }
            public long BansReported { get; set; }

            public bool NeedsReporting()
            {
                return (ProxiesRequested | SuccessesReported | SiteFailuresReported | ProxyFailuresReported | BansReported) != 0;
            }
            public void Clear()
            {
                ProxiesRequested = 0;
                SuccessesReported = 0;
                SiteFailuresReported = 0;
                ProxyFailuresReported = 0;
                BansReported = 0;
            }
        }
        public static Dictionary<Guid, Usage> UsageStatistics = new Dictionary<Guid, Usage>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    foreach(KeyValuePair<Guid, Usage> kvp in UsageStatistics)
                    {
                        if (kvp.Value.NeedsReporting())
                        {
                            var sql = @"
UPDATE APIKey
SET 
ProxiesRequested = ProxiesRequested + " + kvp.Value.ProxiesRequested + @",
SuccessesReported = SuccessesReported + " + kvp.Value.SuccessesReported + @",
SiteFailuresReported = SiteFailuresReported + " + kvp.Value.SiteFailuresReported + @",
ProxyFailuresReported = ProxyFailuresReported + " + kvp.Value.ProxyFailuresReported + @",
BansReported = BansReported + " + kvp.Value.BansReported + @"
WHERE APIKey.APIKey = '" + kvp.Key.ToString() + @"'";
                            using (var conn = new SqlConnection(Configuration.DatabaseConnectionString))
                            {
                                conn.Open();
                                var command = new SqlCommand(sql, conn);
                                var pResult = command.ExecuteScalar();
                            }
                            kvp.Value.Clear();
                        }
                    }


                    await Task.Delay(60000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                // TODO log?
            }
        }
    }
}
