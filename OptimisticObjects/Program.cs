using System;
using System.Threading;

namespace OptimisticObjects
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			/*var test = new TestObject ();
			test.MyProperty = "Some thing here";

			var optimistTest = test.CreateOptimisticObject ();
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
			}*/


			var listing = new OptimisticObject ("http://api.trendsales.dk/2/listings/26601923");
			listing.Subscribe("ItemType", (itemtype) => {
				Console.WriteLine("ItemType: " + itemtype);
			});
			listing.UpdateValues (new { Following = false});
			listing.Subscribe("Following", (itemtype) => {
				Console.WriteLine("Following changed to: " + itemtype + " ("
				                  + (listing.IsSync("Following") ? "sync" : "mock") + ")");
			});
			listing.Sync ();
			Thread.Sleep (1000);
			listing.UpdateValues (new { Following = true }, () => {
				var obj = listing.FillObject<Listing>();
				var action = listing.GetAction<bool>("Follow", obj);
				action();
			});

			while (listing.HasProcesses) {
				listing.Process();
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
