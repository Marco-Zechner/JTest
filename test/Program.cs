using MarcoZechner.JTest;
using MarcoZechner.PrettyReflector;

namespace MarcoZechner.Test;

public class Program
{
    public static async Task Main(string[] args)
    {
        // var foundTests = TestManager.GetTestData();
        // Console.WriteLine($"Found {foundTests.Count} tests.\n" + string.Join("\n", foundTests.Select(test => test.TestName)));
        // var testResults = await TestManager.RunTestsAsync(foundTests);

        // foreach (var testResult in testResults)
        // {
        //     Console.WriteLine($"\nTest {testResult.TestName} = " + testResult.Status.PrettyValue());
        //     if (testResult.Status == Status.Failed)
        //         Console.WriteLine(testResult.Result?.FailMessage);
        //     if (testResult.Status == Status.ExecptionThrow)
        //         Console.WriteLine(testResult.Result?.FailMessage);            
        // }
        // Console.ReadLine();
        await TestManager.InteractiveTestRunner();
    }
}
