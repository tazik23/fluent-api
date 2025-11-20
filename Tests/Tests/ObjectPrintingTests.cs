using FluentAssertions;
using ObjectPrinting;
using ObjectPrinting.PrintingConfigs;
using ObjectPrinting.PrintingConfigs.Extensions;
using Tests.TestEntities;

namespace Tests.Tests;

public class ObjectPrintingTests
{
    private Person person;

    [SetUp]
    public void SetUp()
    {
        person = new Person { Name = "Alex", Age = 19, Height = 175.123 };
    }

    [Test]
    public async Task PrintToString_SimpleObject_ShouldSerializeAllMembers()
    {
        var result = person.PrintToString();
        
        await Verify(result);
    }
    
    [Test]
    public void PrintToString_ExcludedType_ShouldNotSerializeMembersOfThatType()
    {
        var printer = new PrintingConfig<Person>()
            .Excluding<double>();

        var result = printer.PrintToString(person);

        result.Should().NotContain("Height");
    }
    
    [Test]
    public void PrintToString_ExcludedType_ShouldNotSerializeAllMembersOfThatType()
    {
        var obj = new TestClass { Property = "Prop", Field = "Field" };
        
        var printer = ObjectPrinter.For<TestClass>()
            .Excluding<string>();
        
        var result = printer.PrintToString(obj);
        result.Should().NotContain("Prop").And.NotContain("Field");
    }
    
    [Test]
    public void PrintToString_ExcludedProperty_ShouldNotSerializeProperty()
    {
        var printer = new PrintingConfig<Person>()
            .Excluding(p => p.Age);

        var result = printer.PrintToString(person);

        result.Should().NotContain("Age");
    }
    
    [Test]
    public void PrintToString_ExcludedField_ShouldNotSerializeField()
    {
        var obj = new TestClass { Property = "Prop", Field = "Field" };

        var result = obj.PrintToString(c => c.Excluding(o => o.Field));

        result.Should().Contain("Property").And.NotContain("Field");
    }
    
    [Test]
    public void PrintToString_ExcludedMultipleMembers_ShouldNotSerializeMultipleMembers()
    {
        var printer = ObjectPrinter.For<Person>()
            .Excluding(p => p.Id)
            .Excluding(p => p.Age);

        
        var result = printer.PrintToString(person);
        result.Should().NotContain("Id").And.NotContain("Age");
    }
}