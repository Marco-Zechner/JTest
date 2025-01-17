using System.Reflection;

namespace MarcoZechner.JTest; 

public abstract class TestManager {
    private static readonly List<TestCase> testCases;
    private static readonly TestNode codeView;
    private static readonly TestNode categoryView;

    static TestManager()
    {
        testCases = ReflectionHandler.DiscoverTests();
        codeView = BuildCodeView(testCases);
        categoryView = BuildCategoryView(testCases);
    }

    public static List<TestCase> GetTestData()
    {
        return testCases;
    }

    public static async Task<List<TestCase>> RunTestsAsync(List<TestCase> testsToRun)
    {
        var testTasks = testsToRun.Select(async testCase =>
        {
            testCase.Status = Status.Running;
            try
            {
                await ExecuteTestAsync(testCase);
                testCase.Status = Status.Succeeded;
            }
            catch (Exception ex)
            {
                testCase.Status = Status.Failed;
                testCase.Result = new TestResult
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace
                };
            }
        });

        await Task.WhenAll(testTasks);
        return testsToRun;
    }

    public static async Task InteractiveTestRunner()
    {
        var codeRunner = new CodeRunner(codeView, categoryView);
        await codeRunner.Render();
    }

    private static async Task ExecuteTestAsync(TestCase testCase)
    {
        try
        {
            // Use reflection to get the type and method
            var testClassType = Type.GetType($"{testCase.NamespaceName}.{testCase.ClassName}") 
                ?? throw new Exception($"Class {testCase.ClassName} not found.");

            var method = testClassType.GetMethod(testCase.MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) 
                ?? throw new Exception($"Static method {testCase.MethodName} not found in class {testCase.ClassName}.");

            // Prepare parameters
            var parameters = testCase.Parameters.Select(p => p.ParameterValue).ToArray();

            // Check if the method is asynchronous (returns Task or Task<T>)
            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                // Invoke asynchronously
                var task = (Task?)method.Invoke(null, parameters) 
                    ?? throw new Exception($"Method {testCase.MethodName} returned null for Task. Is it a constructor?");

                // Await the task
                await task;
            }
            else
            {
                // Invoke synchronously
                method.Invoke(null, parameters);
            }

            testCase.Status = Status.Succeeded;
            testCase.Result = new TestResult();
        }
        catch (Exception ex)
        {
            testCase.Status = Status.Failed;
            testCase.Result = new TestResult{
                ErrorMessage = $"Error executing test {testCase.CaseName}: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private static TestNode BuildCodeView(List<TestCase> testCases)
    {
        var root = new TestNode { Name = "Root" };

        foreach (var testCase in testCases)
        {
            var namespaceNode = root.Children.FirstOrDefault(x => x.Name == testCase.NamespaceName)
                ?? new TestNode { Name = testCase.NamespaceName };
            if (!root.Children.Contains(namespaceNode))
                root.Children.Add(namespaceNode);

            var classNode = namespaceNode.Children.FirstOrDefault(x => x.Name == testCase.ClassName)
                ?? new TestNode { Name = testCase.ClassName };
            if (!namespaceNode.Children.Contains(classNode))
                namespaceNode.Children.Add(classNode);

            var methodNode = classNode.Children.FirstOrDefault(x => x.Name == testCase.MethodName)
                ?? new TestNode { Name = testCase.MethodName };
            if (!classNode.Children.Contains(methodNode))
                classNode.Children.Add(methodNode);

            methodNode.TestCases.Add(testCase);
        }

        return root;
    }

    private static TestNode BuildCategoryView(List<TestCase> testCases)
    {
        var root = new TestNode { Name = "Root" };

        foreach (var testCase in testCases)
        {
            var categories = testCase.FullCategoryPath.Split('/');
            var currentNode = root;

            foreach (var category in categories)
            {
                var categoryNode = currentNode.Children.FirstOrDefault(x => x.Name == category)
                    ?? new TestNode { Name = category };
                if (!currentNode.Children.Contains(categoryNode))
                    currentNode.Children.Add(categoryNode);

                currentNode = categoryNode;
            }

            currentNode.TestCases.Add(testCase);
        }

        return root;
    }
}