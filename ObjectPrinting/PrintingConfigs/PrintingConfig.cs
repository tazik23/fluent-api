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
    private readonly HashSet<Type> excludedTypes = new();
    private readonly HashSet<MemberInfo> excludedMembers = new();
    private readonly Dictionary<Type, Func<object, string>> typeSerializers = new();
    private readonly Dictionary<MemberInfo, Func<object, string>> memberSerializers = new();
    private readonly Dictionary<Type, CultureInfo> typeCultures = new();
    private readonly Dictionary<MemberInfo, int> trimLengths = new();

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
        excludedTypes.Add(typeof(TProp));
        return this;
    }

    public PrintingConfig<TOwner> Excluding<TProp>(Expression<Func<TOwner, TProp>> selector)
    {
        excludedMembers.Add(GetMember(selector));
        return this;
    }

    public TypePrintingConfig<TOwner, TProp> Printing<TProp>()
    {
        return new TypePrintingConfig<TOwner, TProp>(this);
    }

    public PropertyPrintingConfig<TOwner, TProp> Printing<TProp>(Expression<Func<TOwner, TProp>> selector)
    {
        return new PropertyPrintingConfig<TOwner, TProp>(this, selector);
    }

    public StringPropertyPrintingConfig<TOwner> Printing(Expression<Func<TOwner, string>> selector)
    {
        return new StringPropertyPrintingConfig<TOwner>(this, selector);
    }
    
    public PrintingConfig<TOwner> SetSerializerFor<TProp>(Func<TProp, string> serializer)
    {
        typeSerializers[typeof(TProp)] = p => serializer((TProp)p);
        return this;
    }
    
    public PrintingConfig<TOwner> SetSerializerFor<TProp>(
        Expression<Func<TOwner, TProp>> selector, Func<TProp, string> serializer)
    {
        memberSerializers[GetMember(selector)] = m => serializer((TProp)m);
        return this;
    }

    public PrintingConfig<TOwner> SetCultureFor<TProp>(CultureInfo culture)
        where TProp : IFormattable
    {
        typeCultures[typeof(TProp)] = culture;
        return this;
    }

    public PrintingConfig<TOwner> TrimMember(Expression<Func<TOwner, string>> selector, int trimLength)
    {
        if (trimLength < 0)
        {
            throw new ArgumentException("Max length cannot be less than zero.");
        }
        
        trimLengths[GetMember(selector)] = trimLength;
        return this;
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

        if (excludedTypes.Contains(type))
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

        return FormatObjectByType(obj, nestingLevel, visited, type);
    }
    
    private string FormatObjectByType(object obj, int nestingLevel, HashSet<object> visited, Type type)
    {
        if (TrySerializeType(type, obj, out string? result))
            return result + Environment.NewLine;
    
        if (TryFormatFormattable(obj, type, out result))
            return result;
    
        if (IsFinalType(type))
            return obj + Environment.NewLine;
    
        if (obj is IDictionary dictionary)
            return PrintDictionary(dictionary, nestingLevel, visited);
        
        if (obj is IEnumerable enumerable)
            return PrintEnumerable(enumerable, nestingLevel, visited);
        
        return PrintObject(obj, nestingLevel, visited);
    }
    
        private string PrintObject(object obj, int nestingLevel, HashSet<object> visited)
    {
        var type = obj.GetType();
        var indent = new string('\t', nestingLevel + 1);
        var sb = new StringBuilder();

        sb.AppendLine(type.Name);

        var members = type.GetProperties()
            .Concat(type.GetFields().Cast<MemberInfo>())
            .Where(m => !excludedMembers.Contains(m));

        foreach (var member in members)
        {
            var value = GetMemberValue(obj, member);

            if (value is not null && excludedTypes.Contains(value.GetType()))
            {
                continue;
            }

            sb.Append(indent + member.Name + " = ");

            if (TrySerializeMember(member, value, out var result))
            {
                sb.Append(result).AppendLine();
                continue;
            }

            if (TryTrimStringMember(member, value, out var trimmed))
            {
                sb.Append(trimmed).AppendLine();
                continue;
            }

            sb.Append(PrintToString(value, nestingLevel + 1, visited));
        }

        return sb.ToString();
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
    
    private bool TrySerializeType(Type type, object obj, out string? result)
    {
        if (typeSerializers.TryGetValue(type, out var serializer))
        {
            result = serializer(obj);
            return true;
        }
    
        result = null;
        return false;
    }
    
    private bool TrySerializeMember(MemberInfo member, object? value, out string? result)
    {
        if (memberSerializers.TryGetValue(member, out var serializer))
        {
            result = serializer(value);
            return true;
        }
    
        result = null;
        return false;
    }

    private bool TryFormatFormattable(object obj, Type type, out string result)
    {
        if (obj is IFormattable formattable && typeCultures.TryGetValue(type, out var culture))
        {
            result = Convert.ToString(formattable, culture) + Environment.NewLine;
            return true;
        }
    
        result = string.Empty;
        return false;
    }
    
    private bool TryTrimStringMember(MemberInfo member, object? value, out string? result)
    {
        if (value is string stringValue && trimLengths.TryGetValue(member, out int trimLength))
        {
            result = stringValue.Length > trimLength 
                ? stringValue.Substring(0, trimLength) 
                : stringValue;
            return true;
        }
    
        result = null;
        return false;
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