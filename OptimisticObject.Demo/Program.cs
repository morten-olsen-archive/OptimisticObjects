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
            var demoObject = new OptimisticObjects.OptimisticObject<DemoObject>(string.Empty);
            demoObject.Bind<string>("Name", Bind);

            demoObject.AddChange(new OptimisticObjects.ChangeRequest<DemoObject>{
                Name = "Update Title",
                Values = new Dictionary<string,object> {
                    { "Name", "Hello World 1" }
                },
                Run = Run
            });
            demoObject.AddChange(new OptimisticObjects.ChangeRequest<DemoObject>
            {
                Name = "Update Title",
                Values = new Dictionary<string, object> {
                    { "Name", "Hello World 3" }
                },
                Run = Run
            });
            demoObject.Run();

            Console.ReadKey();
        }

        static void Bind(string input)
        {
            Console.WriteLine("Got value: " + input);
        }

        static OptimisticObjects.OptimisticResponse<DemoObject> Run()
        {
            Thread.Sleep(3000);
            var response = new OptimisticObjects.OptimisticResponse<DemoObject>();
            response.Result = new DemoObject
            {
                Name = "Hello World 2"
            };
            return response;
        }
    }
}
