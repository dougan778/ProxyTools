using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.IO;
using RequestDistribution.Exceptions;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using log4net;
using OpenQA.Selenium.Support.UI;

namespace RequestDistribution
{
    public class DistributionWorker : AbstractWorkerThread
    {
        // Instantiation of ChromeDriver must be atomic against itself, apparently.
        private static Object chromeLock = new Object();

        ILog Log = LogManager.GetLogger("RequestDistribution");
        public int SleepBetweenRequests = 500;
        public bool UseProxies = false;
        public  override string Name { get; set; }
        private IRequestDistributor master;
        private CancellationToken cancellationToken;
        public string Status { get; set; }
        private int contiguousFailureCount = 0;
        private int failureCount = 0;
        private ProxyModel.Proxy proxy = null;

        public DistributionWorker(IRequestDistributor master, string name, CancellationToken token)
        {
            this.Name = name;
            this.master = master;
            this.cancellationToken = token;
        }

        private void RegisterFailure()
        {
            this.contiguousFailureCount++;
            this.failureCount++;
        }

        private void ProxyCheck(Request request)
        {
            if (this.UseProxies && this.master.ProxyRepository != null)
            {
                //TODO I don't think this is actually checking if the proxy has failed this session.  And it should check that.
                if (this.proxy == null || !this.proxy.AvailableForUse || (request.ProxyFilter != null && !this.proxy.MatchesFilter(request.ProxyFilter)))
                {
                    this.proxy?.Discard();
                    this.proxy = this.master.ProxyRepository.GetProxy(request.ProxyFilter);
                }
            }
        }

        protected virtual Response DoHeadlessChromeWebRequest(Request request)
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments(new List<string>() { "--headless" });
            chromeOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36");
            if (proxy != null)
            {
                chromeOptions.Proxy = new OpenQA.Selenium.Proxy() { HttpProxy = proxy.URL, SslProxy = proxy.URL };
            }

            ChromeDriver browser;
            lock (chromeLock)
            {
                browser = new ChromeDriver(chromeOptions);
            }

            try
            {
                browser.Navigate().GoToUrl(request.URL);

                // Wait until all required elements are present, with a timeout.  Slowness of proxies is
                // a consideration in choosing a generous timeout time.
                browser.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 0, 0); // Make it not implicitly wait at all.  We will handle waits explicitly.
                WebDriverWait wait = new WebDriverWait(browser, new TimeSpan(0, 0, 20));
                Func<IWebDriver, bool> waitCondition = GetWaitCondition(request);
                try
                {
                    wait.Until(waitCondition);
                }
                catch(WebDriverTimeoutException)
                {
                    // It timed out.  Do nothing, use what we have as the page source.  If it's incomplete or incorrect,
                    // it's on the requestor to sort that out.
                }

                Response response = new Response(request, browser.PageSource, proxy:proxy as ProxyModel.Proxy);
                //todo check to see if the response actually contained anything.  if not, the proxy failed
                return response;
            }
            catch(Exception ex)
            {
                throw;
            }
            finally
            {
                browser.Quit();
            }
        }

        /// <summary>
        /// Builds a func that indicates what elements for the web driver to wait for on the page before returning the page source.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual Func<IWebDriver, bool> GetWaitCondition(Request request)
        {
            return (IWebDriver driver) =>
            {
                foreach (string xpath in request.ElementsToWaitFor)
                {
                    try
                    {
                        if (driver.FindElements(By.XPath(xpath)).Count == 0)
                        {
                            return false;
                        }
                    }
                    catch(WebDriverException ex)
                    {
                        // I'm not sure that this case happens now that we removed the implicit wait/timeout, 
                        // but if it were to happen, it would mean that it timed out while trying to locate the element.
                        return false;
                    }
                }
                return true;
            };
        }

        protected virtual Response DoRawHTTPWebRequest(Request request)
        {
            this.Status = "Executing web request for: " + request.URL;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(request.URL);
            if (proxy != null)
            {
                WebProxy myProxy = new WebProxy();
                myProxy.Address = new Uri(proxy.URL);
                webRequest.Proxy = myProxy;
            }
            HttpWebResponse rawResponse = (HttpWebResponse)webRequest.GetResponse();
            if (rawResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Bad Status Code: " + rawResponse.StatusCode);
            }
            Stream dataStream = rawResponse.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            Response response = new Response(request, responseFromServer, proxy:proxy as ProxyModel.Proxy);
            return response;
        }
        
        public override void Run()
        {
            try
            {
                Log.Debug("Beginning Request Loop");
                foreach (Request request in this.master.RequestQueue.GetConsumingEnumerable(this.cancellationToken))
                {
                    Log.Debug("Processing Request");

                    if (!this.master.InternetConnectivity)
                    {
                        Log.Debug("No internet connectivity.  Aborting request.");
                        master.EnqueueRequest(request); // Toss it back on the queue.
                        Thread.Sleep(10000);
                        continue;
                    }

                    try
                    {
                        ProxyCheck(request);
                        Response response = null;
                        if (request.DriverType == Request.DriverTypes.RawHTTP)
                        {
                            response = DoRawHTTPWebRequest(request);
                        }
                        else if (request.DriverType == Request.DriverTypes.HeadlessChrome)
                        {
                            response = DoHeadlessChromeWebRequest(request);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        this.contiguousFailureCount = 0;
                        proxy?.RegisterSuccess();
                        this.Status = "Request executed successfully.  Sleeping a bit before the next request"; 
                        // TODO make this count down and check for cancelationtoken.
                        Thread.Sleep(SleepBetweenRequests);
                        master.EnqueueResponse(response);
                    }
                    catch (ProxyRepositoryFailureException ex)
                    {
                        this.Status = "Unable to execute the request.  No available proxies.";
                        // TODO make this count down and check for cancelationtoken.
                        Thread.Sleep(SleepBetweenRequests);
                        master.EnqueueResponse(new Response(request, exception: ex, proxy: proxy));
                    }
                    catch (DriverServiceNotFoundException)
                    {
                        this.Status = "A driver used for scraping (probably chromedriver) is not installed.";
                        throw;
                    }
                    catch (Exception ex)
                    {
                        request.FailureCount++;
                        proxy?.RegisterFailure();
                        this.RegisterFailure();
                        this.Status = "Error: " + ex.Message;

                        // TODO check to see if the URL was bad.  If it is, don't register failure on this thread, just on the request.

                        if (request.FailureCount < Request.MAX_FAILURES)
                        {
                            master.EnqueueRequest(request); // Toss it back on the queue.
                        }
                        else
                        {
                            Response response = new Response(request, exception: ex, proxy: proxy as ProxyModel.Proxy);
                            master.EnqueueResponse(response);
                        }
                    }
                }
                master.EnqueueResponse(this);

            }
            catch (OperationCanceledException) { }
            finally
            {
                this.Status = "Running Ended.";
            }
        }
    }
}
