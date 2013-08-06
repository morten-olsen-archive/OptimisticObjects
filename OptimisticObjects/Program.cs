using System;
using System.Threading;

namespace OptimisticObjects
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var wrapper = new WrapperTest ();
			var test = new TestObject ();
			test.MyProperty = "Some thing here";

			var optimistTest = test.CreateOptimisticObject (() => {
				WorkingServerStuff();
			});
			optimistTest.Failed += (operation, ex) => {
				Console.WriteLine("Object creating failed, doing rollback, to last known sync state");
			};
			//wrapper.BindProperty ("Write", optimistTest, "MyProperty");

			optimistTest.Subscribe ("MyProperty", (value) => {
				Console.WriteLine("MyProperty: " + value + " (" + (optimistTest.IsSync("MyProperty") ? "sync" : "mock") + ")");
			});

			optimistTest.UpdateValues(new {
				MyProperty = "Hello World"
			}, WorkingServerStuff);
			
			optimistTest.UpdateValues(new {
				MyProperty = "Hello asd"
			}, FailingServerStuff);

			while (optimistTest.HasProcesses) {
				optimistTest.Process ();
			}
		}









		public static void WorkingServerStuff() {
			Console.WriteLine ("Server: Success");
		}

		public static void FailingServerStuff() {
			Console.WriteLine ("Server: Failed");
			throw new Exception ();
		}
	}
}
