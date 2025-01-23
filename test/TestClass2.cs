using MarcoZechner.JTest;

namespace MarcoZechner.Test;

public class TestClass2 {
    [Test]
    [TestInfo("Base/Cat1/Cat2")]
    public static void TestMethodWithFailMessage()
    {
        Assert.Same(true, false, "This is a fail message, and this test should fail");
    }
}