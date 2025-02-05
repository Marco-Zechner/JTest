using MarcoZechner.JTest;

namespace MarcoZechner.Test;

[TestInfo("Base")]
public class TestClass
{
    [Test]
    [TestInfo("Cat3")]
    public static void MostBasicTestMethod()
    {
        Assert.Same(true, true);
    }

    [Test]
    [TestInfo("Cat1/Cat2")]
    public static void TestMethodWithFailMessage()
    {
        Assert.Same(true, false, "This is a fail message, and this test should fail");
    }

    [Test]
    public static void TestMethodWithWrongParameter(int param)
    {
        Assert.Same(true, false, "This is a fail message, and this test should fail");
    }

    [Test(Name = "CustomNamesParamterTest")]
    [Case(2, "Hello", Name = "HelloCase")]
    [Case(1, "World", Name = "WorldCase")]
    public static void ParameterTest(int param1, string param2) {
        Assert.Same(param1, param2.Length - 3, "This message is useless. This test too. 1 case should pass, 1 case should fail.");
    }
}