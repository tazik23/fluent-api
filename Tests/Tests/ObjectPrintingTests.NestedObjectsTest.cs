using FluentAssertions;
using ObjectPrinting;
using Tests.TestEntities;

namespace Tests.Tests;

public partial class ObjectPrintingTests
{
    [TestFixture]
    public class NestedObjectsTest
    {
        [Test]
        public void PrintToString_NestedObject_ShouldPrintAllLevels()
        {
            var node = Node.CreateChain(3);
            var printer = ObjectPrinter
                .For<Node>()
                .CreatePrinter();
            
            var result = printer.PrintToString(node);

            result.Should().Contain("level0")
                .And.Contain("level1")
                .And.Contain("level2");
        }
        
        [Test]
        public void PrintToString_WithCyclicReference_ShouldNotFall()
        {
            var node = Node.CreateChainWithCycle();
            var printer = ObjectPrinter
                .For<Node>()
                .CreatePrinter();
            
            var act = () => printer.PrintToString(node);
            
            act.Should().NotThrow();
            act().Should().Contain("cyclic reference");
        }
        
        [Test]
        public void PrintToString_WhenReachesMaxRecursionLevel_ShouldStop()
        {
            var node = Node.CreateChain(16);
            var printer = ObjectPrinter
                .For<Node>()
                .CreatePrinter();
            
            var result = printer.PrintToString(node);

            result.Should()
                .Contain("level15")
                .And.NotContain("level16");
        }
        
        [TestCase(0)]
        [TestCase(-1)]
        public void SetMaxRecursionDepth_NotPositiveValue_ShouldThrowArgumentException(int maxDepth)
        {
            var printer = ObjectPrinter.For<Node>();

            var act = () => printer.SetMaxRecursionDepth(maxDepth);

            act.Should().Throw<ArgumentException>();
        }
        
    }
}