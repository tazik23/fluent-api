using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner>
    {
        internal readonly HashSet<Type> ExcludedTypes = new();
        internal readonly HashSet<MemberInfo> ExcludedMembers = new();

        public PrintingConfig<TOwner> Excluding<TProp>()
        {
            ExcludedTypes.Add(typeof(TProp));
            return this;
        }

        public PrintingConfig<TOwner> Excluding<TProp>(Expression<Func<TOwner, TProp>> selector)
        {
            ExcludedMembers.Add(GetMember(selector));
            return this;
        }

        private static MemberInfo GetMember<TProp>(Expression<Func<TOwner, TProp>> selector)
        {
            if (selector.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member;
            }
            
            throw new ArgumentException("Selector must refer to a property or a field.");
        }
        
        public string PrintToString(TOwner obj)
        {
            return PrintToString(obj, 0);
        }

        private string PrintToString(object obj, int nestingLevel)
        {
            //TODO apply configurations
            if (obj == null)
                return "null" + Environment.NewLine;

            var finalTypes = new[]
            {
                typeof(int), typeof(double), typeof(float), typeof(string),
                typeof(DateTime), typeof(TimeSpan)
            };
            if (finalTypes.Contains(obj.GetType()))
                return obj + Environment.NewLine;

            var identation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();
            var type = obj.GetType();
            sb.AppendLine(type.Name);
            foreach (var propertyInfo in type.GetProperties())
            {
                sb.Append(identation + propertyInfo.Name + " = " +
                          PrintToString(propertyInfo.GetValue(obj),
                              nestingLevel + 1));
            }
            return sb.ToString();
        }
    }
}