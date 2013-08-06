using System;

namespace OptimisticObjects
{
	public class WrapperTest
	{
		public string Write
		{
			set {
				Console.WriteLine ("p=" + value);
			}
		}
	}
}

