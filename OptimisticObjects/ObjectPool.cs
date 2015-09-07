using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimisticObjects
{
    public static class ObjectPool<TType>
        where TType : new()
    {
        static ObjectPool()
        {
            Objects = new Dictionary<string, ObjectStore<TType>>();
        }

        static Dictionary<string, ObjectStore<TType>> Objects { get; set; }

        public static OptimisticObject<TType> Get(string url)
        {
            OptimisticObject<TType> obj = null;
            lock (Objects)
            {
                if (!Objects.ContainsKey(url))
                {
                    Objects.Add(url, new ObjectStore<TType>
                    {
                        RetainCount = 0,
                        Obj = new OptimisticObject<TType>(url)
                    });
                }
                Objects[url].RetainCount++;
                obj = Objects[url].Obj;
            }
            return obj;
        }

        public static void Release(string url)
        {
            if (Objects.ContainsKey(url))
            {
                Objects[url].RetainCount--;
                if (Objects[url].RetainCount <= 0)
                {
                    Objects[url].Obj.ClearBindings();
                    Objects.Remove(url);
                }
            }
        }
    }
}
