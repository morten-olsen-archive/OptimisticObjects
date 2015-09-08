using OptimisticObjects;
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
            var demoTextLabel = new DemoTextLabel();
            var demoObject = OptimisticObjects.ObjectPool<DemoObject>.Get("SomeUrl");
            demoObject.Bind(b => b.Name, demoTextLabel, t => t.Text);


            var demoTextLabelCopy = new DemoTextLabel();
            var demoObjectCopy = OptimisticObjects.ObjectPool<DemoObject>.Get("SomeUrl");
            demoObject.Bind(b => b.Name, demoTextLabelCopy, t => t.Text);

            var change1 = demoObject.RequestChange("Update Title");
            change1.ChangeValue(b => b.Name, "Optimistic Value 1");
            change1.Run = Run;
            demoObject.AddChange(change1);

            var change2 = demoObjectCopy.RequestChange("Update Title");
            change2.ChangeValue(b => b.Name, "Optimistic Value 2");
            change2.ChangeValue(b => b.Count, 123);
            change2.Run = Run;
            demoObjectCopy.AddChange(change2);


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
