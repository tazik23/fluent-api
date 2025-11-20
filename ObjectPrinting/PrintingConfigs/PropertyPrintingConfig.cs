using System;
using System.Reflection;

namespace ObjectPrinting.PrintingConfigs;

public class PropertyPrintingConfig<TOwner, TProp>
{
    protected readonly PrintingConfig<TOwner> Config;
    protected readonly MemberInfo Member;

    public PropertyPrintingConfig(PrintingConfig<TOwner> config, MemberInfo member)
    {
        Config = config;
        Member = member;
    }

    public PrintingConfig<TOwner> Using(Func<TProp, string> serializer)
    {
        Config.MemberSerializers[Member] = m => serializer((TProp)m);
        return Config;
    }
}