using System;
using System.Reflection;

namespace ObjectPrinting.PrintingConfigs;

public class PropertyPrintingConfig<TOwner, TProp>
{
    private readonly PrintingConfig<TOwner> _config;
    private readonly MemberInfo _member;

    public PropertyPrintingConfig(PrintingConfig<TOwner> config, MemberInfo member)
    {
        _config = config;
        _member = member;
    }

    public PrintingConfig<TOwner> Using(Func<TProp, string> serializer)
    {
        _config.MemberSerializers[_member] = m => serializer((TProp)m);
        return _config;
    }
}