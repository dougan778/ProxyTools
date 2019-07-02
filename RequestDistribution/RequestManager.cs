using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProxyModel;

namespace RequestDistribution
{
    public class RequestManager : IDisposable
    {
        protected IRequestDistributor _distributor;
        public IRequestDistributor Distributor { get { return _distributor; } }
        protected ConcurrentDictionary<Request, Action<Response>> _mappings = new ConcurrentDictionary<Request, Action<Response>>();
        protected bool _running = false;
        public int QueueSize { get { return _distributor.RequestQueue.Count; } }

        public long ResultsProcessed = 0L;

        public RequestManager(IRequestDistributor distributor)
        {
            _distributor = distributor;
            Start();
        }

        public RequestManager(IProxyRepository proxyRepository)
        {
            _distributor = new RequestDistributor() { ProxyRepository = proxyRepository };
        }

        public void Start()
        {
            Thread thread = new Thread(HandleResponses);
            thread.Start();
        }

        public void Stop()
        {
            _distributor.StopProcessingRequests();
        }

        private bool _stopped = false;
        public bool Stopped
        {
            get
            {
                return _stopped;
            }
        }
        protected void HandleResponses()
        {
            _stopped = false;
            foreach (Response response in _distributor.ExecuteRequests())
            {
                try
                {
                    Action<Response> handler = null;
                    if (_mappings.TryRemove(response.Request, out handler))
                    {
                        // TODO what to do about exceptions here?
                        handler(response);
                    }
                    else
                    {
                        throw new Exception("A request was encountered that was not a part of the registered requests.");
                    }
                }
                finally
                {
                    ResultsProcessed++;
                }
            }
            _stopped = true;
        }

        /// <summary>
        /// Delegates off the processing of the request.  When the request has been executed, the handler method will be called with the result.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="resultHandler">The method to call with the reponse.  This should be lightweight.</param>
        public void ProcessRequest(Request request, Action<Response> resultHandler)
        {
            _mappings.TryAdd(request, resultHandler);
            _distributor.EnqueueRequest(request);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
