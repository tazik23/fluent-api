using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ObjectPrinting.PrintingConfigs;

namespace ObjectPrinting;

public class ObjectPrinter
{
    private readonly PrinterSettings settings;

    public ObjectPrinter(PrinterSettings settings)
    {
        this.settings = settings;
    }

    public static PrintingConfig<T> For<T>()
    {
        return new PrintingConfig<T>();
    }
        
    public string PrintToString<TOwner>(TOwner obj)
    {
        return PrintToString(obj, 0, new HashSet<object>());
    }

    private string PrintToString(object? obj, int nestingLevel, HashSet<object> visited)
    {
        if (nestingLevel > settings.MaxRecursionDepth)
            return string.Empty;
        
        if (obj is null)
            return "null" + Environment.NewLine;

        var type = obj.GetType();

        if (settings.ExcludedTypes.Contains(type))
            return string.Empty;

        if (IsCyclicReference(obj, type, visited))
            return "(cyclic reference)" + Environment.NewLine;

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
            .Where(m => !settings.ExcludedMembers.Contains(m));

        foreach (var member in members)
        {
            var value = GetMemberValue(obj, member);

            if (value is not null && settings.ExcludedTypes.Contains(value.GetType()))
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
        if (settings.TypeSerializers.TryGetValue(type, out var serializer))
        {
            result = serializer(obj);
            return true;
        }
    
        result = null;
        return false;
    }
    
    private bool TrySerializeMember(MemberInfo member, object? value, out string? result)
    {
        if (settings.MemberSerializers.TryGetValue(member, out var serializer))
        {
            result = serializer(value);
            return true;
        }
    
        result = null;
        return false;
    }

    private bool TryFormatFormattable(object obj, Type type, out string result)
    {
        if (obj is IFormattable formattable && settings.TypeCultures.TryGetValue(type, out var culture))
        {
            result = Convert.ToString(formattable, culture) + Environment.NewLine;
            return true;
        }
    
        result = string.Empty;
        return false;
    }
    
    private bool TryTrimStringMember(MemberInfo member, object? value, out string? result)
    {
        if (value is string stringValue && settings.TrimLengths.TryGetValue(member, out int trimLength))
        {
            result = stringValue.Length > trimLength 
                ? stringValue.Substring(0, trimLength) 
                : stringValue;
            return true;
        }
    
        result = null;
        return false;
    }
    
    private static bool IsCyclicReference(object obj, Type type, HashSet<object> visited)
    {
        return !type.IsValueType && !visited.Add(obj);
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