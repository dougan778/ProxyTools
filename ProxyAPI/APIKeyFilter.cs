
//using ActionFilters.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProxyAPI
{
 
    public class APIKeyFilter : ActionFilterAttribute
    {
        protected static HashSet<Guid> ValidAPIKeys = new HashSet<Guid>();

        protected static bool CheckAPIKey(Guid key)
        {
            lock (ValidAPIKeys)
            {
                if (ValidAPIKeys.Contains(key))
                {
                    return true;
                }
            }

            using (var conn = new SqlConnection(Configuration.DatabaseConnectionString))
            {
                conn.Open();
                var sql = $"SELECT apikey FROM apikey WHERE apikey = '{key.ToString()}'";
                var command = new SqlCommand(sql, conn);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return false;
                    }
                }
            }

            lock (ValidAPIKeys)
            {
                if (!ValidAPIKeys.Contains(key))
                {
                    ValidAPIKeys.Add(key);
                }
                return true;
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
           
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var authHeader = context.HttpContext.Request.Headers.TryGetValue("APIKey", out var values);
                Guid key = new Guid(values[0]);
                if (!CheckAPIKey(key))
                {
                    context.Result = new Http403Result();

                }
            }
            catch (Exception)
            {
                context.Result = new Http403Result();
            }

        }
        internal class Http403Result : ActionResult
        {
            public override void ExecuteResult(ActionContext context)
            {
                context.HttpContext.Response.StatusCode = 403;
            }
        }

    }
}

