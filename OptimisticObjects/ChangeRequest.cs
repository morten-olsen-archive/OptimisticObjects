using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimisticObjects
{
    public class ChangeRequest<TType>
    {
        public string Name { get; set; }
        public Dictionary<string, object> Values { get; set; }
        public Func<OptimisticResponse<TType>> Run { get; set; }
        public int Attempts { get; set; }
    }
}
