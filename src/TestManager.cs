using System.Reflection;
using MarcoZechner.PrettyReflector;

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
            await ExecuteTestAsync(testCase);
        });

        await Task.WhenAll(testTasks);
        return testsToRun;
    }

    public static async Task InteractiveTestRunner()
    {
        var codeRunner = new CodeRunner([codeView, categoryView]);
        await codeRunner.Render();
    }

    private static async Task ExecuteTestAsync(TestCase testCase)
    {
        try
        {
            // Find the assembly that contains the class
            var testAssembly = Assembly.GetEntryAssembly()
                ?? throw new Exception("Entry assembly not found.");

            // Resolve the class type
            var testClassType = testAssembly.GetTypes()
                .FirstOrDefault(type => type.FullName == $"{testCase.NamespaceName}.{testCase.ClassName}")
                ?? throw new Exception($"Class {testCase.ClassName} not found in the assembly.");

            // Resolve the method
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

            testCase.Status = Status.Passed;
            testCase.Result = new TestResult();
        }
        catch (Exception ex)
        {
            var assertException = ex is AssertException assertEx
                ? assertEx
                : ex.InnerException as AssertException;

            if (assertException != null) {
                testCase.Status = Status.Failed;
                testCase.Result = new TestResult{
                    AssertException = assertException,
                    FailMessage = "Assert failed:".SetLength(16).CombineLines($"{assertException.Message}\n{assertException.StackTrace}", "  ")
                };
                return;
            }

            // Handle general exceptions
            testCase.Status = Status.ExecptionThrow;
            var innerException = ex.InnerException;

            testCase.Result = new TestResult
            {
                FailMessage = "Exception:".SetLength(16).CombineLines($"{ex.Message}\n{ex.StackTrace}", "  ")
            };

            if (innerException != null){
                string innerFailMessage = $"Inner Exception:".SetLength(16).CombineLines($"{innerException.Message}\n{innerException.StackTrace}", "  ");
                testCase.Result.FailMessage += "\n" + innerFailMessage;
            }
        }
    }

    public static string EscapeSpecialCharactersRegex(string input)
{
    if (string.IsNullOrEmpty(input))
        return string.Empty;

    return System.Text.RegularExpressions.Regex.Replace(input, @"[\a\b\f\n\r\t\v\\\""]", match =>
    {
        return match.Value switch
        {
            "\a" => "\\a",
            "\b" => "\\b",
            "\f" => "\\f",
            "\n" => "\\n",
            "\r" => "\\r",
            "\t" => "\\t",
            "\v" => "\\v",
            "\\" => "\\\\",
            "\"" => "\\\"",
            _ => match.Value
        };
    });
}

    private static TestNode BuildCodeView(List<TestCase> testCases)
    {
        var root = new TestNode { Name = "CodeView" };

        testCases = [.. testCases.OrderBy(testCase => testCase.TestName)];
        foreach (var testCase in testCases)
        {
            string[] namespaceSections = testCase.NamespaceName.Split('.');

            var currentNode = root;
            var namespaceNode = root;

            foreach (var section in namespaceSections)
            {
                namespaceNode = currentNode.Children.FirstOrDefault(x => x.Name == section)
                    ?? new TestNode { Name = section };
                if (!currentNode.Children.Contains(namespaceNode))
                    currentNode.Children.Add(namespaceNode);

                currentNode = namespaceNode;
            }

            var classNode = namespaceNode.Children.FirstOrDefault(x => x.Name == testCase.ClassName)
                ?? new TestNode { Name = testCase.ClassName };
            if (!namespaceNode.Children.Contains(classNode))
                namespaceNode.Children.Add(classNode);

            if (testCase.IsDefault)
            {
                classNode.TestCases.Add(testCase);
                continue;
            }
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
        var root = new TestNode { Name = "CategoryView" };

        testCases = [.. testCases.OrderBy(testCase => testCase.TestName).OrderBy(testCase => testCase.FullCategoryPath)];
        foreach (var testCase in testCases)
        {
            var categories = testCase.FullCategoryPath.Split('/');
            string? testCaseHolder = null;
            int otherCaseIndex = testCases.FindIndex(x => x != testCase && x.FullCategoryPath == testCase.FullCategoryPath && x.MethodName == testCase.MethodName);
            if (otherCaseIndex != -1) {
                testCaseHolder = testCase.MethodName;
            }
                
            var currentNode = root;

            foreach (var category in categories)
            {
                string cateogoryName = category;
                if (string.IsNullOrEmpty(category))
                    cateogoryName = "Uncategorized";
                var categoryNode = currentNode.Children.FirstOrDefault(x => x.Name == cateogoryName)
                    ?? new TestNode { Name = cateogoryName };
                if (!currentNode.Children.Contains(categoryNode))
                    currentNode.Children.Add(categoryNode);

                currentNode = categoryNode;
            }
            if (testCaseHolder != null)
            {
                var holderNode = currentNode.TestCasesHolder.FirstOrDefault(x => x.Name == testCaseHolder)
                    ?? new TestNode { Name = testCaseHolder };
                if (!currentNode.TestCasesHolder.Contains(holderNode))
                    currentNode.TestCasesHolder.Add(holderNode);

                currentNode = holderNode;
            }

            currentNode.TestCases.Add(testCase);
        }

        return root;
    }
}