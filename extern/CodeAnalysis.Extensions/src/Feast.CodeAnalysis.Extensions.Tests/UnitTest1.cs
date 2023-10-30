using Generators;

namespace Feast.CodeAnalysis.Extensions.Tests;
using AnotherName = AnotherClass;

[RelateTo(typeof(AnotherName))]
public partial class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}
