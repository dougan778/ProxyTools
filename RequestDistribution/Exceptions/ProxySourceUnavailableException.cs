using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestDistribution.Exceptions
{
    class ProxySourceUnavailableException : Exception
    {
        public ProxySourceUnavailableException(string message = null) : base(message) { }
    }
}
