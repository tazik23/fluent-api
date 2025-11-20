namespace Tests.Tests;

public partial class ObjectPrintingTests
{
    public partial class CollectionTests
    {
        private class CollectionTestsClass
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public List<CollectionTestsClass> Friends { get; set; } = new();
        }
    }
}