using MarcoZechner.ColorString;
using MarcoZechner.JTest;
using MarcoZechner.PrettyReflector;

namespace MarcoZechner.Test;

public class Program
{
    public static async Task Main(string[] args)
    {
        // string multiLineText = "This is a multi-line text\n" +
        //     "that spans multiple lines\n" +
        //     "and is assigned to a variable.";

        // Console.WriteLine("Title:".CombineLines(multiLineText, " -|- "));

        var foundTests = TestManager.GetTestData();
        Console.WriteLine($"Found {foundTests.Count} tests.");
        var testResults = await TestManager.RunTestsAsync(foundTests);

        foreach (var testResult in testResults)
        {
            ColoredConsole.WriteLine($"\nTest {testResult.TestName} = {testResult.Status.ColoredPrettyValue()}");
            if (testResult.Status == Status.Failed)
                Console.WriteLine(testResult.Result?.AssertException);
            if (testResult.Status == Status.ExecptionThrow)
                ColoredConsole.WriteLine(testResult.Result?.FailMessage);            
        }
    }
}
