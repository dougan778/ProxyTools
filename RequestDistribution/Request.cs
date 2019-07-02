using System;
using System.Collections.Generic;
using System.Text;
using ProxyModel;

namespace RequestDistribution
{
    public class Request
    {
        public enum DriverTypes
        {
            RawHTTP,
            HeadlessChrome
        }
        public const int MAX_FAILURES = 5;
        public string URL { get; set; }
        public int FailureCount { get; set; }

        /// <summary>
        /// List of XPaths of elements to wait for to indicate that the page has loaded.
        /// </summary>
        public List<string> ElementsToWaitFor { get; set; } = new List<string>();

        public Request(string url)
        {
            this.URL = url;
        }


        public void RegisterFailure()
        {
            this.FailureCount++;
        }

        private DriverTypes _driverType = DriverTypes.RawHTTP;
        public DriverTypes DriverType
        {
            get
            {
                return _driverType;
            }
            set
            {
                _driverType = value;
            }
        }

        public ProxyFilter ProxyFilter { get; set; }
    }
}
