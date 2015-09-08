using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimisticObject.Demo
{
    public class DemoTextLabel
    {
        public string Text
        {
            get { return null; }
            set { Console.WriteLine("Label:\t" + value); }
        }
    }
}
