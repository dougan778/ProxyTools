using System;
using System.Collections.Generic;
using System.Text;

namespace RequestDistribution
{
    public abstract class AbstractWorkerThread
    {
        protected System.Threading.Thread _thread = null;

        public abstract void Run();

        public abstract string Name { get; set; }
        public void Start()
        {
            this._thread = new System.Threading.Thread(this.Run);
            this._thread.Name = Name;
            this._thread.Start();
        }

    }
}
