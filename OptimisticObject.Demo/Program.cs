using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OptimisticObject.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var demoObject = OptimisticObjects.ObjectPool<DemoObject>.Get("SomeUrl");
            demoObject.Bind(b => b.Name, (string val) =>
            {
                Console.WriteLine("Original\t: " + val);
            });

            var demoObjectCopy = OptimisticObjects.ObjectPool<DemoObject>.Get("SomeUrl");
            demoObjectCopy.Bind(b => b.Name, (string val) =>
            {
                Console.WriteLine("Copy\t\t: " + val);
            });



            demoObject.AddChange(new OptimisticObjects.ChangeRequest<DemoObject>{
                Name = "Update Title",
                Values = new Dictionary<string,object> {
                    { "Name", "Optimistic Value 1" }
                },
                Run = Run
            });



            demoObject.AddChange(new OptimisticObjects.ChangeRequest<DemoObject>
            {
                Name = "Update Title",
                Values = new Dictionary<string, object> {
                    { "Name", "Optimistic Value 2" }
                },
                Run = Run
            });



            demoObject.Run().ContinueWith((t) =>
            {
                OptimisticObjects.ObjectPool<DemoObject>.Release("SomeUrl");
                OptimisticObjects.ObjectPool<DemoObject>.Release("SomeUrl");
                Console.WriteLine("Done");
            });

            Console.ReadKey();
        }

        static OptimisticObjects.OptimisticResponse<DemoObject> Run()
        {
            var response = new OptimisticObjects.OptimisticResponse<DemoObject>();
            Console.WriteLine("- Sync state received");
            response.Result = new DemoObject
            {
                Name = "Pessimistic Value 1 - Sync"
            };
            return response;
        }
    }
}
