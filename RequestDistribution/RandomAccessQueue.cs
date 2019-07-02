using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestDistribution
{
    public class RandomAccessQueue<T> : List<T>
    {
        public T Dequeue()
        {
            T result = this[0];
            this.RemoveAt(0);
            return result;
        }

        public void Enqueue(T item)
        {
            Add(item);
        }
    }
}
