using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using log4net;
using System.Net.NetworkInformation;
using System.Timers;
using ProxyModel;

namespace RequestDistribution
{
    /// <summary>
    /// Handles delegating requests to a team of worker threads, and collecting all of the responses into a single generator method.
    /// </summary>
    public class RequestDistributor  : IRequestDistributor
    {
        ILog Log = LogManager.GetLogger("RequestDistribution");
        public BlockingCollection<Request> RequestQueue { get; set; }
        public List<DistributionWorker> Workers { get; set; }
        public BlockingCollection<Object> ResponseQueue { get; set; }
        private CancellationTokenSource workerCancellationSource = new CancellationTokenSource();
        public IProxyRepository ProxyRepository { get; set; }
        /// <summary>
        /// This indicates whether or not the distributor can properly access the internet.  If this is false, workers 
        /// should hold off from processing requests (becuase they will presumably fail and mess up proxy scores).
        /// </summary>
        public bool InternetConnectivity { get; private set; } = true;
        protected System.Timers.Timer InternetConnectivityTimer = null;

        public RequestDistributor()
        {
            this.RequestQueue = new BlockingCollection<Request>();
            this.ResponseQueue = new BlockingCollection<Object>();
            this.Workers = new List<DistributionWorker>();
        }

        /// <summary>
        /// Adds a distribution worker that will distribute its requests across proxies.  
        /// </summary>
        public void AddProxiedWorker()
        {
            DistributionWorker worker = new DistributionWorker(this, "(Proxied) worker #" + this.Workers.Count, workerCancellationSource.Token);
            worker.UseProxies = true;
            worker.SleepBetweenRequests = 500;
            this.Workers.Add(worker);
        }

        /// <summary>
        /// Adds a distribution worker that does not utilize proxies and will initiate its requests with the local IP,
        /// and in order to protect itself, throttles its requests to be slower.  In the future, something to throttle by
        /// hostname would allow this to process more requests by switching between hosts.
        /// </summary>
        public void AddThrottledWorker()
        {
            DistributionWorker worker = new DistributionWorker(this, "(Throttled) worker #" + this.Workers.Count, workerCancellationSource.Token);
            worker.UseProxies = false;
            worker.SleepBetweenRequests = 10000;
            this.Workers.Add(worker);
        }

        private void InitializeWorkers()
        {
            if (this.Workers.Count == 0)
            {
                throw new Exception("No distribution workers were set up.");
            }
            foreach(var worker in this.Workers)
            {
                worker.Start();
            }
        }
        
        public void EnqueueRequest(Request request)
        {
            RequestQueue.Add(request);
        }

        public void EnqueueResponse(Object response)
        {
            ResponseQueue.Add(response);
        }

        /// <summary>
        /// Stops processing requests.  Allows workers to complete their current request and will continue to yield
        /// results of any in-process requests before the distributor shuts down.
        /// </summary>
        public void StopProcessingRequests()
        {
            // Ask all workers to stop through the cancellation token, which will trickle down into the generator stopping.
            if (!this.workerCancellationSource.IsCancellationRequested)
            { 
                this.workerCancellationSource.Cancel();
            }

            if (InternetConnectivityTimer != null)
            {
                InternetConnectivityTimer.Enabled = false;
                InternetConnectivityTimer.Dispose();
            }
        }
        
        public IEnumerable<Response> ExecuteRequests()
        {
            InternetConnectivityCheck();
            InitiateInternetConnectivityCheckInterval();
            Log.Debug("Executing Requests.");
            var distributorCancellationSource = new CancellationTokenSource();
            try
            {
                this.InitializeWorkers();

                foreach (Object produce in this.ResponseQueue.GetConsumingEnumerable(distributorCancellationSource.Token))
                {
                    if (produce is Response response)
                    {
                        string successString = response.Body != null ? "Y" : "N";
                        Log.Info($"Response yielded.  Success? {successString} URL: {response.Request.URL} ");
                        yield return response;
                    }
                    else if (produce is DistributionWorker worker)
                    {
                        this.Workers.Remove(worker);
                    }

                    if (this.Workers.Count == 0 && this.ResponseQueue.Count == 0)
                    {
                        Log.Debug("No workers or responses present.");
                        ResponseQueue.CompleteAdding();
                    }
                }
            }
            finally
            {
                Log.Debug("Stopping execution of this thread.");   
                this.StopProcessingRequests(); 
            }
        }

        /// <summary>
        /// Starts a timer that periodically checks for internet connectivity and toggles the InternetConnectivity flag accordingly.
        /// </summary>
        protected void InitiateInternetConnectivityCheckInterval()
        {
            InternetConnectivityTimer = new System.Timers.Timer();
            InternetConnectivityTimer.Elapsed += new ElapsedEventHandler((object source, ElapsedEventArgs e) =>
            {
                InternetConnectivityCheck();
            });
            InternetConnectivityTimer.Interval = 30000;
            InternetConnectivityTimer.Enabled = true;
        }

        protected void InternetConnectivityCheck()
        {
            Ping ping;
            try
            {
                using (ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8");
                    InternetConnectivity = reply.Status == IPStatus.Success;
                    Log.Info("Internet Connectivity Ping Result: " + (InternetConnectivity ? "success" : "fail"));
                }
            }
            catch (PingException)
            {
                InternetConnectivity = false;
                Log.Info("Internet Connectivity Ping Failed due to an exception.");
            }
        }
    }
}


