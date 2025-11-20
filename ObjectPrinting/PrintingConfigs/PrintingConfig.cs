using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ObjectPrinting.PrintingConfigs;

public class PrintingConfig<TOwner>
{
    private int maxRecursionDepth = 16;
    
    internal readonly HashSet<Type> ExcludedTypes = new();
    internal readonly HashSet<MemberInfo> ExcludedMembers = new();
    internal readonly Dictionary<Type, Func<object, string>> TypeSerializers = new();
    internal readonly Dictionary<MemberInfo, Func<object, string>> MemberSerializers = new();
    internal readonly Dictionary<Type, CultureInfo> TypeCultures = new();
    internal readonly Dictionary<MemberInfo, int> TrimLengths = new();

    public PrintingConfig<TOwner> SetMaxRecursionDepth(int recursionDepth)
    {
        if (recursionDepth <= 0)
        {
            throw new ArgumentException("Max recursion depth must be greater than zero.");
        }
        maxRecursionDepth = recursionDepth;
        return this;
    }
    
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

    public TypePrintingConfig<TOwner, TProp> Printing<TProp>()
    {
        return new TypePrintingConfig<TOwner, TProp>(this);
    }

    public PropertyPrintingConfig<TOwner, TProp> Printing<TProp>(Expression<Func<TOwner, TProp>> selector)
    {
        return new PropertyPrintingConfig<TOwner, TProp>(this, GetMember(selector));
    }

    public StringPropertyPrintingConfig<TOwner> Printing(Expression<Func<TOwner, string>> selector)
    {
        return new StringPropertyPrintingConfig<TOwner>(this, GetMember(selector));
    }
    
    public string PrintToString(TOwner obj)
    {
        return PrintToString(obj, 0, new HashSet<object>());
    }

    private string PrintToString(object? obj, int nestingLevel, HashSet<object> visited)
    {
        if (nestingLevel > maxRecursionDepth)
        {
            return string.Empty;
        }
        
        if (obj is null)
        {
            return "null" + Environment.NewLine;
        }

        var type = obj.GetType();

        if (ExcludedTypes.Contains(type))
        {
            return string.Empty;
        }

        if (!type.IsValueType)
        {
            if (visited.Contains(obj))
            {
                return "(cyclic reference)" + Environment.NewLine;
            }

            visited.Add(obj);
        }

        if (TypeSerializers.TryGetValue(type, out var typeSerializer))
        {
            return typeSerializer(obj) + Environment.NewLine;
        }

        if (obj is IFormattable)
        {
            if (TypeCultures.TryGetValue(type, out var culture))
            {
                return Convert.ToString(obj, culture) + Environment.NewLine;
            }
        }

        if (IsFinalType(type))
        {
            return obj + Environment.NewLine;
        }

        if (obj is IDictionary dictionary)
        {
            return PrintDictionary(dictionary, nestingLevel, visited);
        }
        
        if (obj is IEnumerable enumerable)
        {
            return PrintEnumerable(enumerable, nestingLevel, visited);
        }
        
        return PrintObject(obj, nestingLevel, visited);
    }

    private string PrintObject(object obj, int nestingLevel, HashSet<object> visited)
    {
        var type = obj.GetType();
        var indent = new string('\t', nestingLevel + 1);
        var sb = new StringBuilder();

        sb.AppendLine(type.Name);

        var members = type.GetProperties()
            .Concat(type.GetFields().Cast<MemberInfo>());

        foreach (var member in members)
        {
            if (ExcludedMembers.Contains(member))
                continue;

            var value = GetMemberValue(obj, member);
            
            if(value is not null && ExcludedTypes.Contains(value.GetType()))
            {
                continue;
            }

            sb.Append(indent + member.Name + " = ");
            
            if (MemberSerializers.TryGetValue(member, out var memberSerializer))
            {
                sb.Append(memberSerializer(value)).AppendLine();
                continue;
            }
            
            if (value is string stringValue && TrimLengths.TryGetValue(member, out int trimLength))
            {
                sb.Append(stringValue.Length > trimLength
                        ? stringValue.Substring(0, trimLength)
                        : stringValue)
                    .AppendLine();
                continue;
            }
            
            sb.Append(PrintToString(value, nestingLevel + 1, visited));
        }

        return sb.ToString();
    }


    private object? FormatMemberValue(object value, MemberInfo memberInfo)
    {
        if (value is string stringValue && TrimLengths.TryGetValue(memberInfo, out int trimLength))
        {
            if (stringValue.Length > trimLength)
            {
                return stringValue.Substring(0, trimLength);
            }
        }

        return value;
    }

    private string PrintEnumerable(IEnumerable enumerable, int nestingLevel, HashSet<object> visited)
    {
        var indent = new string('\t', nestingLevel + 1);
        var sb = new StringBuilder();

        sb.AppendLine("[");

        foreach (var item in enumerable)
        {
            sb.Append(indent)
                .Append(PrintToString(item, nestingLevel + 1, visited));
        }

        sb.Append(new string('\t', nestingLevel)).AppendLine("]");
        return sb.ToString();
    }
    
    private string PrintDictionary(IDictionary dict, int nesting, HashSet<object> visited)
    {
        var indent = new string('\t', nesting + 1);
        var sb = new StringBuilder();

        sb.AppendLine("{");

        foreach (DictionaryEntry entry in dict)
        {
            var key = entry.Key;
            var value = entry.Value;

            sb.Append(indent + "[");
            sb.Append(PrintToString(key, nesting + 1, visited).TrimEnd());
            sb.Append("] = ");
            sb.Append(PrintToString(value, nesting + 1, visited));
        }

        sb.Append(new string('\t', nesting)).AppendLine("}");
        return sb.ToString();
    }

    private static bool IsFinalType(Type type)
    {
        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(DateTime)
               || type == typeof(TimeSpan)
               || type == typeof(decimal)
               || type == typeof(Guid);
    }
    
    private static MemberInfo GetMember<TProp>(Expression<Func<TOwner, TProp>> selector)
    {
        if (selector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member;
        }

        throw new ArgumentException("Selector must refer to a property or a field.");
    }
    
    private static object? GetMemberValue(object? obj, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo p => p.GetValue(obj),
            FieldInfo f => f.GetValue(obj),
            _ => throw new InvalidOperationException($"Unsupported member type: {member.MemberType}")
        };
    }
}