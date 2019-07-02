using System;
using System.Collections.Generic;
using System.Text;

namespace RequestDistribution.Exceptions
{
    public class ProxyRepositoryFailureException : Exception
    {
        public ProxyRepositoryFailureException() : base() {  }
        public ProxyRepositoryFailureException(string message) : base(message) { }
        public ProxyRepositoryFailureException(string message, Exception ex) : base(message, ex) { }
    }
}
