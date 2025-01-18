using MarcoZechner.JTest;

namespace MarcoZechner.Test;

public class TestClass
{
    [Test]
    public static void MostBasicTestMethod()
    {
        Assert.Same(true, true);
    }

    [Test]
    public static void TestMethodWithFailMessage()
    {
        Assert.Same(true, false, "This is a fail message, and this test should fail");
    }

    [Test]
    public static void TestMethodWithWrongParameter(int param)
    {
        Assert.Same(true, false, "This is a fail message, and this test should fail");
    }
}