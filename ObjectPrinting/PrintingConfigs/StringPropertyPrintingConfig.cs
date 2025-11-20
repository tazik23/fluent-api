using System;
using System.Reflection;

namespace ObjectPrinting.PrintingConfigs;

public class StringPropertyPrintingConfig<TOwner> : PropertyPrintingConfig<TOwner, string>
{
    public StringPropertyPrintingConfig(PrintingConfig<TOwner> config, MemberInfo member) : base(config, member)
    {
    }

    public PrintingConfig<TOwner> TrimToLength(int maxLength)
    {
        if (maxLength < 0)
        {
            throw new ArgumentException("Max length cannot be less than zero.");
        }
        
        Config.TrimLengths[Member] = maxLength;
        
        return Config;
    }
}