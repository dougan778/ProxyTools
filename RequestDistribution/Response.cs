using System;
using System.Collections.Generic;
using System.Text;

namespace RequestDistribution
{
    public class Response
    {
        /// <summary>
        /// The initial request that was executed to get this response.
        /// </summary>
        public Request Request { get; set; }
        public string Body { get; set; }
        public Exception Exception { get; set; }
        public ProxyModel.Proxy Proxy { get; set; }

        public bool Success {  get { return this.Exception == null; } }

        public Response(Request request, string body = null, Exception exception = null, ProxyModel.Proxy proxy = null)
        {
            this.Request = request;
            this.Body = body;
            this.Exception = exception;
            this.Proxy = proxy;
        }
    }
}
