using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;

namespace RequestDistribution
{
    public interface IRequestDistributor
    {
        BlockingCollection<Request> RequestQueue { get; }
        List<DistributionWorker> Workers { get; set; }
        BlockingCollection<Object> ResponseQueue { get; set; }
        void EnqueueResponse(Object response);
        void EnqueueRequest(Request request);
        IEnumerable<Response> ExecuteRequests();
        ProxyModel.IProxyRepository ProxyRepository { get; }
        void StopProcessingRequests();
        void AddThrottledWorker();
        void AddProxiedWorker();
        bool InternetConnectivity { get; }

    }
}
