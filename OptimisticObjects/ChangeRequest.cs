using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OptimisticObjects
{
    public class ChangeRequest<TType>
    {
        public ChangeRequest(string name)
        {
            Name = name;
            Values = new Dictionary<string, object>();
        }

        public string Name { get; set; }
        internal Dictionary<string, object> Values { get; set; }
        public Func<OptimisticResponse<TType>> Run { get; set; }
        internal int Attempts { get; set; }

        public void ChangeValue<T>(Expression<Func<TType, T>> property, T value)
        {
            var propertyInfo = ((MemberExpression)property.Body).Member as PropertyInfo;
            Values[propertyInfo.Name] = value;
        }
    }
}
