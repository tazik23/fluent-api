using FluentAssertions;
using ObjectPrinting;

namespace Tests.Tests;

public partial class ObjectPrintingTests
{
    [TestFixture]
    public partial class CollectionTests
    {
        private class Department
        {
            public string Name { get; set; }
        }

        [Test]
        public void PrintToString_Array_ShouldPrintEachElement()
        {
            var data = new[] { 1, 2, 3 };
            var printer = ObjectPrinter.For<int[]>().CreatePrinter();
            
            var result = printer.PrintToString(data);
            
            result.Should()
                .Contain("[")
                .And.Contain("1")
                .And.Contain("2")
                .And.Contain("3")
                .And.Contain("]");
        }

        [Test]
        public void PrintToString_ArrayOfObjects_ShouldSerializeEachElement()
        {
            var data = new[]
            {
                new CollectionTestsClass { Name = "Alice", Age = 25 },
                new CollectionTestsClass { Name = "Bob", Age = 30 }
            };
            var printer = ObjectPrinter.For<CollectionTestsClass[]>().CreatePrinter();
            
            var result = printer.PrintToString(data);

            result.Should().Contain("[")
                .And.Contain("CollectionTestsClass", Exactly.Twice())
                .And.Contain("]");
        }
        
        [Test]
        public void PrintToString_Dictionary_ShoulPrintKeysAndValues()
        {
            var dict = new Dictionary<int, string>
            {
                [1] = "1",
                [2] = "2"
            };

            var printer = ObjectPrinter.For<Dictionary<int, string>>().CreatePrinter();
            var result = printer.PrintToString(dict);

            result.Should()
                .Contain("[1] = 1")
                .And.Contain("[2] = 2");
        }


        [Test]
        public void PrintToString_DictionaryWithComplexObjects_ShouldSerializeKeysAndValues()
        {
            var dict = new Dictionary<CollectionTestsClass, Department>
            {
                [new CollectionTestsClass { Name = "Manager" }] = new Department { Name = "HR" },
                [new CollectionTestsClass { Name = "Developer" }] = new Department { Name = "IT" }
            };

            var printer = ObjectPrinter
                .For<Dictionary<CollectionTestsClass, Department>>()
                .CreatePrinter();
            var result = printer.PrintToString(dict);
            
            result.Should().MatchRegex(
                @"\[\s*CollectionTestsClass[\s\S]*?\]\s*=\s*Department[\s\S]*?" +
                @"\[\s*CollectionTestsClass[\s\S]*?\]\s*=\s*Department");
        }

        [Test]
        public void PrintToString_EmptyCollection_ShouldShowEmptyStructure()
        {
            var data = new CollectionTestsClass { Name = "Test", Friends = new List<CollectionTestsClass>() };
            var printer = ObjectPrinter
                .For<CollectionTestsClass>()
                .CreatePrinter();
            var result = printer.PrintToString(data);

            result.Should().Contain("Friends = [");
        }

        [Test]
        public void PrintToString_NullCollection_ShouldShowNull()
        {
            var data = new CollectionTestsClass() { Name = "Test", Friends = null };
            var printer = ObjectPrinter
                .For<CollectionTestsClass>()
                .CreatePrinter();
            var result = printer.PrintToString(data);

            result.Should().Contain("Friends = null");
        }
    }
}