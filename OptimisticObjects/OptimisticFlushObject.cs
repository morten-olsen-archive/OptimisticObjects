using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace OptimisticObjects
{
	public class OptimisticFlushObject : OptimisticObject
	{
		public OptimisticFlushObject () : base()
		{
		}

		public OptimisticFlushObject (string uri) : base(uri)
		{
			Sync ();
		}

		public void SetValue(object value)
		{
			var stackTrace = new StackTrace();
			var frames = stackTrace.GetFrames();
			var thisFrame = frames[1];
			var method = thisFrame.GetMethod();
			var methodName = method.Name; // Should be get_* or set_*
			var name = method.Name.Substring(4);
			SetObject(name, value);
		}

		private Dictionary<string, object> _values;

		public T GetValue<T>()
		{
			var stackTrace = new StackTrace();
			var frames = stackTrace.GetFrames();
			var thisFrame = frames[1];
			var method = thisFrame.GetMethod();
			var methodName = method.Name; // Should be get_* or set_*
			var name = method.Name.Substring(4);
			return (T)OptimisticValues [name];
		}

		public void SetObject(string name, object value)
		{
			UpdateOptimisticValues (name, value);
		}

		public void Save()
		{
			var action = GetAction<object> ("update", GetDiff ());
		}
	}
}

