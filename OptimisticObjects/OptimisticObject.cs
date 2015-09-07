using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Net.Http;
using Newtonsoft.Json;

namespace OptimisticObjects
{
    public class OptimisticObject<TType>
        where TType : new()
    {
        public OptimisticObject(string url)
        {
            ResourceUrl = url;
            PessimisticValues = new Dictionary<string, object>();
            PendingChanges = new List<ChangeRequest<TType>>();
            Bindings = new Dictionary<string, List<object>>();
            Actions = new Dictionary<string, string>();
        }

        private string ResourceUrl { get; set; }
        private Dictionary<string, List<object>> Bindings { get; set; }
        private Dictionary<string, object> PessimisticValues { get; set; }
        private Dictionary<string, string> Actions { get; set; }
        private List<ChangeRequest<TType>> PendingChanges { get; set; }

        public void Bind<T>(string name, Action<T> binding)
        {
            if (!Bindings.ContainsKey(name))
            {
                Bindings[name] = new List<object>();
            }
            Bindings[name].Add(binding);
        }

        public void Unbind<T>(string name, Action<T> binding)
        {
            if (!Bindings.ContainsKey(name))
            {
                Bindings[name] = new List<object>();
            }
            Bindings[name].Remove(binding);
        }

        private TType GetStaleVersion(Dictionary<string, object> values)
        {
            var response = default(TType);
            var type = response.GetType();

            foreach (var item in values)
            {
                var prop = type.GetRuntimeProperty(item.Key);
                if (prop != null)
                {
                    prop.SetValue(response, item.Value);
                }
            }

            return response;
        }

        public TType GetPessimisticVersion(Dictionary<string, object> values)
        {
            return GetStaleVersion(PessimisticValues);
        }

        public void AddChange(ChangeRequest<TType> change)
        {
            PendingChanges.Add(change);
            NotifyChanges();
        }

        public void ApplyObject(TType obj)
        {
            var type = obj.GetType();
            lock (PessimisticValues)
            {
                foreach (var item in type.GetRuntimeProperties())
                {
                    PessimisticValues[item.Name] = item.GetValue(obj);
                }
            }
            NotifyChanges();
        }

        public Dictionary<string, object> GetOptimisticValues()
        {
            Dictionary<string, object> baseValues = null;
                baseValues = PessimisticValues.ToDictionary(entry => entry.Key,
                                                   entry => entry.Value);
            foreach (var changeRequest in PendingChanges)
            {
                foreach (var item in changeRequest.Values)
                {
                    baseValues[item.Key] = item.Value;
                }
            }

            return baseValues;
        }

        private void CallBinding (string name, object value)
        {
            foreach (var binding in Bindings[name])
            {
                var gType = binding.GetType().GenericTypeArguments[0];
                var type = typeof(Action<>).MakeGenericType(gType);
                var t = type.GetRuntimeMethod("Invoke", new[] { gType });
                t.Invoke(binding, new object[] { value });
            }
        }

        public void NotifyChanges()
        {
            var values = GetOptimisticValues();
            foreach (var binding in Bindings)
            {
                if (values.ContainsKey(binding.Key))
                {
                    CallBinding(binding.Key, values[binding.Key]);
                }
                else
                {
                    
                }
            }
        }

        public async Task Update()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(ResourceUrl).ConfigureAwait(true);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                var obj = JsonConvert.DeserializeObject<TType>(body);
                ApplyObject(obj);
            }
        }

        private bool running = false;
        public async Task Run()
        {
            if (running) return;

            running = true;
            await Task.Run(() =>
            {
                while (PendingChanges.Any())
                {
                    ChangeRequest<TType> change = null;
                    lock (PendingChanges)
                    {
                        change = PendingChanges.First();
                        PendingChanges.Remove(change);
                    }
                    var response = change.Run();
                    if (response.Error != null)
                    {
                        if (change.Attempts < 3)
                        {
                            change.Attempts++;
                            lock (PendingChanges)
                            {
                                PendingChanges.Insert(0, change);
                            }
                        }
                        else
                        {
                            if (response.Error != null)
                            {
                                lock (PendingChanges)
                                {
                                    PendingChanges.Clear();
                                }
                            }
                        }
                    }
                    else
                    {
                        ApplyObject(response.Result);
                    }
                }
                running = false;
            });
        }

        internal void ClearBindings()
        {
            lock (Bindings)
            {
                Bindings.Clear();
            }
        }
    }
}
