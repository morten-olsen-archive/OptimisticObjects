using System;
using System.Collections;
using System.Diagnostics;

namespace OptimisticObjects
{
	public class Listing : OptimisticFlushObject
	{
		public Listing (int listingId) : base("http://api.trendsales.dk/2/listings/" + listingId.ToString())
		{
			
		}

		public string ItemType {
			get {
				return GetOptimisticValue<string> ();
			}
			set {
				SetOptimisticValue (value);
			}
		}
	}
}

