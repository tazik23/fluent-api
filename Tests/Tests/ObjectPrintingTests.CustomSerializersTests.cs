using System.Globalization;
using FluentAssertions;
using ObjectPrinting;
using ObjectPrinting.PrintingConfigs.Extensions;

namespace Tests.Tests;

public partial class ObjectPrintingTests
{
    [TestFixture]
    public class CustomSerializerTests
    {
        private class CustomSerializerTestsClass
        {
            public Guid Id { get; set; }
            public string Name { get; set; }    
            public int Age { get; set; }
            public decimal Price { get; set; }
            public decimal Discount { get; set; }
        }

        private CustomSerializerTestsClass testData;

        [SetUp]
        public void SetUp()
        {
            testData = new CustomSerializerTestsClass
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                Name = "John",
                Age = 25,
                Price = 19.99m,
                Discount = 0.1m
            };
        }

        [Test]
        public void PrintToString_CustomTypeSerializer_ShouldFormatType()
        {
            var printer = ObjectPrinter.For<CustomSerializerTestsClass>()
                .Printing<Guid>().Using(g => g
                    .ToString("N").Substring(0, 8).ToUpper())
                .CreatePrinter();

            var result = printer.PrintToString(testData);

            result.Should().Contain("Id = A1B2C3D4");
        }

        [Test]
        public void PrintToString_CustomPropertySerializer_ShouldOverrideTypeSerializer()
        {
            var printer = ObjectPrinter.For<CustomSerializerTestsClass>()
                .Printing<int>().Using(i => $"{i} years")
                .Printing(p => p.Age).Using(age => $"Age: {age}")
                .CreatePrinter();

            var result = printer.PrintToString(testData);

            result.Should().Contain("Age = Age: 25")
                .And.NotContain("Age = 25 years");
        }

        [Test]
        public void PrintToString_CustomSerializerReturnsNull_ShouldHandleCorrectly()
        {
            var printer = ObjectPrinter.For<CustomSerializerTestsClass>()
                .Printing(p => p.Name).Using(_ => null)
                .CreatePrinter();

            var result = printer.PrintToString(testData);

            result.Should().Contain("Name = ");
        }

        [Test]
        public void PrintToString_CustomSerializerForMultipleTypes_ShouldApplyAll()
        {
            var printer = ObjectPrinter.For<CustomSerializerTestsClass>()
                .Printing<Guid>().Using(g => g.ToString().Substring(0, 8))
                .Printing<int>().Using(i => $"#{i}")
                .Printing<decimal>().Using(p => $"${p}")
                .CreatePrinter();

            var result = printer.PrintToString(testData);

            result.Should()
                .Contain("Id = a1b2c3d4")
                .And.Contain("Age = #25")
                .And.Contain("Price = $19,99");
        }

        [Test]
        public void PrintToString_CustomSerializerForOneProperty_ShouldNotAffectOtherProperties()
        {
            var printer = ObjectPrinter.For<CustomSerializerTestsClass>()
                .Printing(p => p.Price).Using(p => $"${p}")
                .CreatePrinter();

            var result = printer.PrintToString(testData);

            result.Should().Contain("Price = $19,99")
                .And.Contain("Discount = 0,1") 
                .And.Contain("Name = John"); 
        }

        [Test]
        public void PrintToString_MultiplePropertySerializers_ShouldApplyEachToRespectiveProperty()
        {
            var printer = ObjectPrinter.For<CustomSerializerTestsClass>()
                .Printing(p => p.Name).Using(n => $"{n.ToUpper()}")
                .Printing(p => p.Age).Using(a => $"{a} years")
                .Printing(p => p.Price).Using(p => $"${p}")
                .CreatePrinter();

            var result = printer.PrintToString(testData);

            result.Should().Contain("Name = JOHN")
                .And.Contain("Age = 25 years")
                .And.Contain("Price = $19,99");
        }
        
        [Test]
        public void PrintToString_CustomSerializerDominatesOverCulture()
        {
            var data = new { Price = 1234.56d, Weight = 7.89d };
            var printer = ObjectPrinter.For<object>()
                .Printing<double>().Using(new CultureInfo("de-DE"))
                .Printing<double>().Using(p => $"${p:0.00}")
                .CreatePrinter();

            var result = printer.PrintToString(data);

            result.Should().Contain("Price = $1234,56")
                .And.Contain("Weight = $7,89");
        }
    }
}