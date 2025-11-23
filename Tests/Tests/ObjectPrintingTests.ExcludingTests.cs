using FluentAssertions;
using ObjectPrinting;
using ObjectPrinting.PrintingConfigs;
using ObjectPrinting.PrintingConfigs.Extensions;
using Tests.TestEntities;

namespace Tests.Tests;

public partial class ObjectPrintingTests
{
    [TestFixture]
    public class ExcludingTests
    {
        private Person person;

        [SetUp]
        public void SetUp()
        {
            person = new Person { Name = "Alex", Age = 19, Height = 175.123 };
        }


        [Test]
        public void PrintToString_ExcludedType_ShouldNotSerializeMembersOfThatType()
        {
            var printer = new PrintingConfig<Person>()
                .Excluding<double>()
                .CreatePrinter();

            var result = printer.PrintToString(person);

            result.Should().NotContain("Height");
        }

        [Test]
        public void PrintToString_ExcludedType_ShouldNotSerializeAllMembersOfThatType()
        {
            var obj = new TestClass { Property = "Prop", field = "Field" };

            var printer = ObjectPrinter.For<TestClass>()
                .Excluding<string>()
                .CreatePrinter();

            var result = printer.PrintToString(obj);
            result.Should().NotContain("Prop").And.NotContain("Field");
        }

        [Test]
        public void PrintToString_ExcludedProperty_ShouldNotSerializeProperty()
        {
            var printer = new PrintingConfig<Person>()
                .Excluding(p => p.Age)
                .CreatePrinter();

            var result = printer.PrintToString(person);

            result.Should().NotContain("Age");
        }

        [Test]
        public void PrintToString_ExcludedField_ShouldNotSerializeField()
        {
            var obj = new TestClass { Property = "Prop", field = "Field" };

            var result = obj.Print(c => c.Excluding(o => o.field));

            result.Should().Contain("Property").And.NotContain("Field");
        }

        [Test]
        public void PrintToString_ExcludedMultipleMembers_ShouldNotSerializeMultipleMembers()
        {
            var printer = ObjectPrinter.For<Person>()
                .Excluding(p => p.Id)
                .Excluding(p => p.Age)
                .CreatePrinter();
            
            var result = printer.PrintToString(person);
            result.Should().NotContain("Id").And.NotContain("Age");
        }

        [Test]
        public void Excluding_WhenNotMemberProvided_ShouldThrowArgumentException()
        {
            var printer = ObjectPrinter.For<Person>();

            var act = () => printer.Excluding(p => p.GetHashCode());

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void PrintToString_AllPropertiesExcluded_ShouldShowTypeName()
        {
            var printer = ObjectPrinter.For<Person>()
                .Excluding<string>()
                .Excluding<int>()
                .Excluding(p => p.Id)
                .Excluding(p => p.Height)
                .CreatePrinter();

            var result = printer.PrintToString(person);

            result.Should().Be("Person" + Environment.NewLine);
        }
    }
}   