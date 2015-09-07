using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimisticObjects
{
    internal class ObjectStore<TType>
            where TType : new()
    {
        public int RetainCount { get; set; }
        public OptimisticObject<TType> Obj { get; set; }
    }
}
