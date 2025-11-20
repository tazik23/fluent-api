using FluentAssertions;
using ObjectPrinting;

namespace Tests.Tests;

public partial class ObjectPrintingTests
{
    [TestFixture]
    public class TrimTests
    {
        private class TrimTestsClass
        {
            public string Short { get; set; }
            public string Long { get; set; }
            public string Description { get; set; }
        }

        [Test]
        public void PrintToString_TrimToZeroLength_ShouldReturnEmptyString()
        {
            var data = new TrimTestsClass { Long = "Very long text" };
            var printer = ObjectPrinter.For<TrimTestsClass>()
                .Printing(p => p.Long).TrimToLength(0);

            var result = printer.PrintToString(data);

            result.Should().Contain("Long = ")
                .And.NotContain("Very");
        }
        
        [Test]
        public void PrintToString_TrimMultipleStringProperties_ShouldTrimEachIndependently()
        {
            var data = new TrimTestsClass
            {
                Short = "Short",
                Long = "Very long text here",
                Description = "Medium description"
            };
            var printer = ObjectPrinter.For<TrimTestsClass>()
                .Printing(p => p.Short).TrimToLength(3)
                .Printing(p => p.Long).TrimToLength(10)
                .Printing(p => p.Description).TrimToLength(6);

            var result = printer.PrintToString(data);

            result.Should().Contain("Short = Sho")
                .And.Contain("Long = Very long ")
                .And.Contain("Description = Medium");
        }
        
        [Test]
        public void TrimToLength_NotPositiveValue_ShouldThrowArgumentException()
        {
            var printer = ObjectPrinter.For<TrimTestsClass>();
            
            var act = () => printer.Printing(p => p.Short).TrimToLength(-1);
            
            act.Should().Throw<ArgumentException>();
        }
    }
}

