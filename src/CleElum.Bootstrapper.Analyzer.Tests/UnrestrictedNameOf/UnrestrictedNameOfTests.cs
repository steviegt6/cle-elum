namespace CleElum.Bootstrapper.Analyzer.Tests.UnrestrictedNameOf; 

[TestFixture]
public static class UnrestrictedNameOfTests {
    private class A {
        private int B;
    }
    
    [TestCase(nameof(A), "A")]
    [TestCase(nameof(A.B), "B")]
    public static void TestNameOf(string nameOf, string actual) {
        Assert.AreEqual(nameOf, actual);
    }
}
