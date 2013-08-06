using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OptimisticObjects
{
	public static class OptimisticObjectExt
	{
		public static OptimisticObject CreateOptimisticObject(this object obj, Action intent) {
			var oObj = new OptimisticObject ();
			oObj.UpdateValues(obj, intent);
			return oObj;
		}

		public static void BindMethod(this object org, string methodName, OptimisticObject obj, string name)
		{
			obj.Subscribe (name, (value) => {
				var m = org.GetType().GetMethod(methodName);
				m.Invoke(org, new[] { value });
			});
		}

		public static void BindProperty(this object org, string propertyName, OptimisticObject obj, string name)
		{
			obj.Subscribe (name, (value) => {
				var m = org.GetType().GetProperty(propertyName);
				m.SetValue(org, value, null);
			});
		}
	}

	public class OptimisticObject
	{
		private const int MaxTries = 5;
		private string _uri;

		public delegate void FailedHandler (Operation operation, Exception ex);
		public event FailedHandler Failed;

		public OptimisticObject ()
		{
			_optimisticValues = new Dictionary<string, object> ();
			_pessimisticValues = new Dictionary<string, object> ();
			_operations = new List<Operation> ();
			_subscriptions = new Dictionary<string, List<Action<object>>> ();
		}

		public OptimisticObject(string uri) : this() {
			_uri = uri;
		}

		public void Sync()
		{
			var client = new WebClient ();
			client.Headers ["Type"] = "application/json";
			var data = client.DownloadString (new Uri(_uri));
			var result = JsonConvert.DeserializeObject<Dictionary<string, object>> (data);
			var inner = result ["Result"];
			var item = (Dictionary<string, object>)JsonConvert.DeserializeObject<Dictionary<string, object>> (inner.ToString());
			
			UpdateOptimisticValues (item);
			UpdatePessimisticValues (item);
		}

		private Dictionary<string, object> _optimisticValues;
		private Dictionary<string, object> _pessimisticValues;
		private List<Operation> _operations;
		private Dictionary<string, List<Action<object>>> _subscriptions;

		public bool IsSync(string name)
		{
			if (!_optimisticValues.ContainsKey (name) &&
				!_pessimisticValues.ContainsKey (name)) {
				return true;
			} else if (!_optimisticValues.ContainsKey (name) ||
			           !_pessimisticValues.ContainsKey (name)) {
				return false;
			}
			else if (_optimisticValues [name] != _pessimisticValues [name]) {
				return false;
			}
			return true;
		}

		public Dictionary<string, object> GetDiff() 
		{
			var diff = new Dictionary<string, object> ();
			foreach (var item in _optimisticValues) {
				if (item.Value != _pessimisticValues[item.Key]) {
					diff.Add (item.Key, item.Value);
				}
			}
			return diff;
		}

		public void UpdateValues(object values, Action intent)
		{
			_operations.Add (new Operation {
				Intent = intent,
				Value = values
			});
			UpdateOptimisticValues (values);
		}

		public void BindProperty(object obj, string property, string name) {
			Subscribe (name, (v) => {
				obj.GetType ().GetProperty (property).SetValue (obj, v, null);
			});
		}

		public void Subscribe(string name, Action<object> action)
		{
			if (!_subscriptions.ContainsKey (name)) {
				_subscriptions.Add(name, new List<Action<object>>());
			}
			_subscriptions [name].Add (action);
			if (_optimisticValues.ContainsKey (name))
				action (_optimisticValues [name]);
		}

		public void Unsubscribe(string name, Action<object> action)
		{
			if (_subscriptions.ContainsKey(name))
				_subscriptions [name].Remove (action);
		}

		public void UpdateOptimisticValues(string name, object value)
		{
			if (_optimisticValues.ContainsKey (name)) {
				_optimisticValues [name] = value;
				NotifySubscribers (name);
			} else {
				_optimisticValues [name] = value;
				NotifySubscribers (name);
			}
		}

		public void UpdateOptimisticValues(object values)
		{
			foreach (var prop in values.GetType().GetProperties()) {
				if (_optimisticValues.ContainsKey (prop.Name)) {
					_optimisticValues [prop.Name] = prop.GetValue (values, null);
					NotifySubscribers (prop.Name);
				} else {
					_optimisticValues [prop.Name] = prop.GetValue (values, null);
					NotifySubscribers (prop.Name);
				}
			}
		}

		private void UpdateOptimisticValues(Dictionary<string, object> values)
		{
			foreach (var prop in values) {
				if (_optimisticValues.ContainsKey (prop.Key)) {
					_optimisticValues [prop.Key] = prop.Value;
					NotifySubscribers (prop.Key);
				} else {
					_optimisticValues [prop.Key] = prop.Value;
					NotifySubscribers (prop.Key);
				}
			}
		}

		private void NotifySubscribers(string name)
		{
			if (!_subscriptions.ContainsKey (name))
				return;
			var value = _optimisticValues [name];
			foreach (var subscriber in _subscriptions[name]) {
				subscriber (value);
			}
		}

		private void UpdatePessimisticValues(object values)
		{
			foreach (var prop in values.GetType().GetProperties()) {
				if (_pessimisticValues.ContainsKey (prop.Name)) {
					_pessimisticValues [prop.Name] = prop.GetValue (values, null);
				} else {
					_pessimisticValues [prop.Name] = prop.GetValue (values, null);
				}
			}
		}

		private void UpdatePessimisticValues(Dictionary<string, object> values)
		{
			foreach (var prop in values) {
				if (_pessimisticValues.ContainsKey (prop.Key)) {
					_pessimisticValues [prop.Key] = prop.Value;
				} else {
					_pessimisticValues [prop.Key] = prop.Value;
				}
			}
		}

		public bool HasProcesses
		{
			get {
				return _operations.Any ();
			}
		}

		public void Process()
		{
			var operation = _operations [0];
			try {
				operation.Intent();
				UpdatePessimisticValues(operation.Value);
				_operations.Remove(operation);
			} catch (Exception ex) {
				++operation.Tries;
				if (operation.Tries >= MaxTries) {
					_operations.Remove (operation);
					_optimisticValues = new Dictionary<string, object>();
					foreach (var val in _pessimisticValues) {
						UpdateOptimisticValues (val.Key, val.Value);
					}
					if (operation.Breaking) {
						_operations.RemoveRange (0, _operations.Count);
					}
					if (Failed != null) {
						Failed (operation, ex);
					}
				}
			}
		}

		public class Operation
		{
			public Operation()
			{
				Breaking = true;
			}

			public Action Intent { get; set; }
			public bool Breaking { get; set; }
			public object Value { get; set; }
			public int Tries { get; set; }
		}
	}
}

