using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using ProxyModel;
using RequestDistribution;

namespace RequestDistributionExample
{
    class RequestDistributionExample
    {

        public void ScrapeExamplePages()
        {
            RequestManager manager = BuildRequestManager();

            foreach (Request request in GetPagesToScrape())
            {
                manager.ProcessRequest(request, ProcessResponse);
            }
        }

        /// <summary>
        /// Create "requests" which are descriptions of what pages to scrape, and how.
        /// </summary>
        protected IEnumerable<Request> GetPagesToScrape()
        {
            var request = new Request("http://www.github.com");
            // You can select a raw HTTP request, which will just return the result of the request, or you can
            // choose to load the page in Chrome.
            request.DriverType = Request.DriverTypes.HeadlessChrome;
            // When using Chrome, you can specify a list of elements whose presence will indicate that the
            // page has fully loaded.  Without them, the request will just be given a long timeout to ensure
            // that everything has loaded.  So this can help with maximizing the rate processing by eliminating
            // the need to wait for the timeout after the necessary components are loaded.
            request.ElementsToWaitFor = new List<string>() { @"//*/button[contains(text(), 'Sign up for GitHub') and contains(@class, 'btn-primary-mktg')]" };

            // Initialize the filter for the proxy.  You can use this to micromanage the quality of the
            // proxy you are getting.  In this case it's just making sure that it doesn't get a proxy
            // that has been overused recently in the current proxy session.
            ProxyFilter pf = new ProxyFilter();
            pf.NotCurrentSession = true;
            request.ProxyFilter = pf;
            yield return request;

            // Return some more of the same, to demonstrate the way requests are distributed amongst proxies.
            for (var i = 0; i < 15; i++)
            {
                yield return new Request("http://www.github.com") { DriverType = Request.DriverTypes.HeadlessChrome, ElementsToWaitFor = new List<string>() { @"//*/button[contains(text(), 'Sign up for GitHub') and contains(@class, 'btn-primary-mktg')]" }, ProxyFilter = pf };
            }

        }

        protected void ProcessResponse(Response response)
        {
            Console.WriteLine($"Response Processed: {response.Request.URL} Success? {(response.Success ? "Y" : "N")} Body Length: {(response.Body?.Length ?? 0)} Proxy: {(response.Proxy?.URL ?? "N/A")}");
            if (response.Exception != null)
            {
                Console.WriteLine($"Error: {response.Exception.Message}");
            }
        }

        /// <summary>
        /// Create a RequestManager.  This object is used to handle requests, passing them to the request
        /// distribution logic and passing the results to the proper handler.
        /// </summary>
        protected RequestManager BuildRequestManager()
        {
            var requestDistributionConnectionString = System.Configuration.ConfigurationManager.AppSettings["ProxyDatabaseConnectionString"].ToString();

            if (requestDistributionConnectionString == null)
            {
                throw new Exception("RequestDistribution database connection string not specified in configuration.");
            }
            var manager = new RequestManager(ProxyRepository.GetStandardRepository(requestDistributionConnectionString, 5));
            manager.Distributor.AddProxiedWorker();
            manager.Distributor.AddProxiedWorker();
            manager.Distributor.AddProxiedWorker();
            manager.Distributor.AddProxiedWorker();
            manager.Distributor.AddProxiedWorker();
            manager.Distributor.AddProxiedWorker();
            manager.Distributor.AddThrottledWorker();
            manager.Distributor.AddThrottledWorker();
            manager.Start();
            return manager;
        }
    }
}
