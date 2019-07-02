using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ProxyAPI.Controllers
{
    [APIKeyFilter]
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        public class ProxyModel
        {
            public ProxyModel() { }
            public ProxyModel(SqlDataReader reader)
            {
                URL = reader.GetString(0);
                ProxyScore =  reader.GetInt32(1);
                Country = reader.IsDBNull(2) ? null : reader.GetString(2);
                Streak = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                ProxyID = reader.GetInt64(4);
                Source = reader.GetString(5);

                if (reader.FieldCount > 6)
                {
                    Site = reader.GetString(6);
                    SiteScore = reader.GetInt32(7);
                }
                SiteVetted = SiteScore != null;
            }
            public long ProxyID;
            public string URL;
            public int ProxyScore;
            public string Country;
            public int Streak;
            public string Site = null;
            public int? SiteScore = null;
            public string Source;
            public bool SiteVetted;
        }
        // GET api/values 
        /// <summary>
        /// This will get proxies for a given site.  Some of the proxies returned will be proxies that have been vetted for the site
        /// in question and are known to be successful, and others will be proxies that have not yet been vetted.  Proxies
        /// that have been vetted and found to fail for this site (or in general) will be excluded.  If the site/sitescore is
        /// null in the response, that indicates an unvetted proxy.
        /// </summary>
        /// <param name="site"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        [HttpGet("List/{site}/{quantity}")]
        public ActionResult<IEnumerable<ProxyModel>> List(string site, int quantity)
        {
            // http://localhost:56917/api/proxy/sears.com/4
            if (quantity > 200) quantity = 200;
            try
            {
                List<ProxyModel> results = new List<ProxyModel>();
                using (var conn = new SqlConnection(Configuration.DatabaseConnectionString))
                {
                    // Locate some vetted ones first
                    // TODO This query is different in MySQL, for when this gets migrated https://stackoverflow.com/questions/580639/how-to-randomly-select-rows-in-sql
                    var sql = @"
SELECT TOP " + (quantity / 2) + @" 
p.URL, p.score as 'ProxyScore', p.country, p.streak, p.proxyid, p.source, pss.site, pss.score as 'SiteScore'
FROM Proxy p
INNER JOIN ProxySiteScore pss ON pss.proxyid = p.proxyid
WHERE p.score >= 0
AND pss.site = '" + site + @"'
AND pss.banned = 0 
AND pss.score >= 0
ORDER BY NEWID()";
                    conn.Open();
                    SqlCommand command = new SqlCommand(sql, conn);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ProxyModel(reader));
                        }
                    }

                    //select some that are not vetted yet.
                    sql = @"
SELECT TOP " + (quantity - results.Count) + @" 
p.URL, p.score as 'ProxyScore', p.country, p.streak, p.proxyid, p.source
FROM Proxy p
WHERE p.score >= 0
and not exists(
select * from proxysitescore pss
where pss.proxyid = p.proxyid
)
ORDER BY NEWID()";
                    command = new SqlCommand(sql, conn);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ProxyModel(reader));
                        }
                    }
                }

                var authHeader = HttpContext.Request.Headers.TryGetValue("APIKey", out var values);
                Guid key = new Guid(values[0]);
                if (!UsageStatisticsService.UsageStatistics.ContainsKey(key))
                {
                    UsageStatisticsService.UsageStatistics.Add(key, new UsageStatisticsService.Usage());
                }
                UsageStatisticsService.UsageStatistics[key].ProxiesRequested += results.Count;
                return results;
            }
            catch(Exception ex)
            {
                return StatusCode(500, ex.Message + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// This will retrieve a specified quantity of proxies.  The proxies will be proxies that have either been
        /// vetted and known to be working, or proxies that have not been vetted yet.  Proxies that have been
        /// vetted and known to fail will be omitted.
        /// </summary>
        /// <param name="quantity"></param>
        /// <returns></returns>
        [HttpGet("List/{quantity}")]
        public ActionResult<IEnumerable<ProxyModel>> Get(int quantity)
        {
            var authHeader = HttpContext.Request.Headers.TryGetValue("APIKey", out var values);
            Guid key = new Guid(values[0]);
            if (!UsageStatisticsService.UsageStatistics.ContainsKey(key))
            {
                UsageStatisticsService.UsageStatistics.Add(key, new UsageStatisticsService.Usage());
            }
            try
            {
                if (quantity > 200) quantity = 200;
                List<ProxyModel> results = new List<ProxyModel>();

                using (var conn = new SqlConnection(Configuration.DatabaseConnectionString))
                {
                    conn.Open();
                    var sql = @"
SELECT TOP " + (quantity - results.Count) + @" 
p.URL, p.score as 'ProxyScore', p.country, p.streak, p.proxyid,  p.source
FROM Proxy p
WHERE p.score > 0
ORDER BY NEWID()";
                    var command = new SqlCommand(sql, conn);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ProxyModel(reader));
                        }
                    }
                }
                UsageStatisticsService.UsageStatistics[key].ProxiesRequested += results.Count;
                return results;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message + "\n" + ex.StackTrace);
            }
            
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] JObject value)
        {
            var successes = 0;
            var failures = 0;
            var banned = false;
            string site = null;
            int score = 0;

            var authHeader = HttpContext.Request.Headers.TryGetValue("APIKey", out var values);
            Guid key = new Guid(values[0]);
            if (!UsageStatisticsService.UsageStatistics.ContainsKey(key))
            {
                UsageStatisticsService.UsageStatistics.Add(key, new UsageStatisticsService.Usage());
            }

            if (value.ContainsKey("successes"))
            {
                int.TryParse(value["successes"].ToString(), out successes);
            }
            if (value.ContainsKey("failures"))
            {
                int.TryParse(value["failures"].ToString(), out failures);
            }
            if (successes > failures)
            {
                score = 1;
            }
            else if (failures > successes)
            {
                score = -1;
            }
            if (value.ContainsKey("banned"))
            {
                banned = value["banned"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            if (value.ContainsKey("site"))
            {
                site = value["site"].ToString();
            }

            var requestSuccesses = successes;
            var requestFailures = failures;
            var requestBanned = banned; 
            
            if (site != null)
            {
                using (var conn = new SqlConnection(Configuration.DatabaseConnectionString))
                {
                    bool update = false;
                    conn.Open();
                    var sql = $"SELECT banned, successes, failures, score FROM proxysitescore WHERE proxyid = {id} and site = '{site}'";
                    var command = new SqlCommand(sql, conn);
                    using (var reader = command.ExecuteReader())
                    {
                        update = reader.Read();
                        if (update)
                        {
                            banned |= bool.Parse(reader.GetValue(0).ToString());
                            successes += Int32.Parse(reader.GetValue(1).ToString());
                            failures += Int32.Parse(reader.GetValue(2).ToString());
                            score += Int32.Parse(reader.GetValue(3).ToString());
                        }
                    }
                    if (update)
                    {
                        sql = $"UPDATE ProxySiteScore SET banned = {(banned ? 1 : 0)}, successes = {successes}, failures = {failures}, score = {score}   WHERE ProxyID = {id} AND Site = '{site}'";
                        command = new SqlCommand(sql, conn);
                        var insertReader = command.ExecuteScalar();
                    }
                    else
                    {
                        sql = $"INSERT INTO ProxySiteScore (proxyid, site, banned, successes, failures, score) VALUES ({id}, '{site}', {(banned ? 1 : 0)}, {successes}, {failures}, {score})";
                        command = new SqlCommand(sql, conn);
                        var pssResult = command.ExecuteScalar();
                    }
                }
            }

            using (var conn = new SqlConnection(Configuration.DatabaseConnectionString))
            {
                conn.Open();
                // Update the proxy itself.
                var sql = "SELECT totalsuccesses, totalfailures, score FROM Proxy where ProxyID = " + id;
                var command = new SqlCommand(sql, conn);
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    successes += Int32.Parse(reader.GetValue(0).ToString());
                    failures += Int32.Parse(reader.GetValue(1).ToString());
                    if (site != null && score < 0) { score = 0; } // Failures are being attributed to the site here, not the proxy.
                    else if (banned) { score = -1; } // If reporting against a proxy as a whole, "ban" isn't really a thing but we can ding the score.
                    score += Int32.Parse(reader.GetValue(2).ToString());
                    if (score > 4) { score = 4; } // Max proxy score is 4.
                }
                sql = $"UPDATE Proxy SET totalsuccesses = {successes}, totalfailures = {failures}, score = {score} WHERE proxyid = {id}";
                command = new SqlCommand(sql, conn);
                var pResult = command.ExecuteScalar();
            }

            // Report usage statistics
            UsageStatisticsService.UsageStatistics[key].SuccessesReported += requestSuccesses;
            if (site != null)
            {
                UsageStatisticsService.UsageStatistics[key].SiteFailuresReported += requestFailures;
                UsageStatisticsService.UsageStatistics[key].BansReported += requestBanned ? 1 : 0;
            }
            else
            {
                UsageStatisticsService.UsageStatistics[key].ProxyFailuresReported += requestFailures;
            }
            return NoContent();
        }
    }
}
