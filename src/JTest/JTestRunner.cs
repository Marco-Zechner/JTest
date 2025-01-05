using System;
using System.Diagnostics;
using System.Reflection;

namespace MarcoZechner.JTest {
    public static class JTestRunner
    {
        private static List<TestResult> _testResults = [];
        private static bool _isRunning = true;
        private static Assembly _testAssembly = Assembly.GetEntryAssembly() 
        ?? throw new InvalidOperationException("Unable to load the entry assembly.");


        private static void StartDisplayThread()
        {
            Task.Run(() =>
            {
                while (_isRunning)
                {
                    lock (_testResults)
                    {
                        DisplayResults();
                    }
                    Thread.Sleep(20);
                }
            });
        }

        public static async Task RunTestsAsync()
        {
            LoadTests();

            // Display initial status
            StartDisplayThread();

            foreach (var test in _testResults)
            {
                // Mark test as running
                lock (_testResults)
                {
                    test.Status = TestStatus.Running;
                }

                if (test.Cases.Count == 0)
                {
                    // Run single test without cases
                    await RunTestMethodAsync(test);
                }
                else
                {
                    // Run test cases in parallel
                    var tasks = test.Cases.Select(testCase => RunTestCaseAsync(test, testCase));
                    await Task.WhenAll(tasks); // Wait for all test cases to complete
                    test.Duration = test.Cases.Max(c => c.Duration);
                }

                lock (_testResults)
                {
                    // Collapse passed cases
                    CollapsePassedCases(test);
                }
            }

            _isRunning = false;
            DisplayResults();
            
            Console.WriteLine("\nPress any key to interact, then press 'q' to quit.");

            Console.ReadKey();

            DisplayInteractiveResults();
        }

        private static void LoadTests()
        {
            // Entry assembly is "Test"
            _testAssembly = Assembly.GetEntryAssembly() 
            ?? throw new InvalidOperationException("Unable to load the entry assembly.");

            var testMethods = _testAssembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                .OrderBy(m => m.GetCustomAttribute<TestAttribute>()?.Order ?? 0);

            foreach (var method in testMethods)
            {
                var testAttr = method.GetCustomAttribute<TestAttribute>();
                var testResult = new TestResult
                {
                    TestName = testAttr?.Name ?? method.Name,
                    Status = TestStatus.Pending
                };

                // Load cases
                var caseAttrs = method.GetCustomAttributes<CaseAttribute>();
                foreach (var caseAttr in caseAttrs)
                {
                    var testCase = new CaseResult
                    {
                        CaseName = caseAttr.Name ?? "Case",
                        Parameters = string.Join(", ", caseAttr.Parameters),
                        ParametersString = caseAttr.Parameters.Select(p => p.ToString() ?? "null").ToArray(),
                        Status = TestStatus.Pending
                    };

                    testResult.Cases.Add(testCase);

                    var methodInfo = FindTestMethod(testResult.TestName);
                    object[]? parameters = ParseParameters(methodInfo.GetParameters(), testCase.Parameters);

                    testCase.ParametersString = parameters.Select(FormatParameter).ToArray();
                }

                testResult.TotalCases = Math.Max(testResult.Cases.Count, 1);
                
                _testResults.Add(testResult);
            }
        }

        private static string FormatParameter(object parameter) {
            if (parameter == null)
            {
                return "null";
            }

            if (parameter is System.Collections.IDictionary dictionary)
            {
                return "{" + string.Join(", ", dictionary.Cast<System.Collections.DictionaryEntry>()
                    .Select(entry => $"{entry.Key}: {entry.Value}")) + "}";
            }

            // Handle collections explicitly
            if (parameter is System.Collections.IEnumerable enumerable && parameter is not string)
            {
                return "[" + string.Join(", ", enumerable.Cast<object>().Select(FormatParameter)) + "]";
            }
            
            string simpleString = parameter.ToString() ?? "null";

            // Use ToString for custom objects or primitives
            return simpleString;
        }

        private static async Task RunTestMethodAsync(TestResult test)
        {
            // var timeout = GetTestTimeout(test); // $"Test timed out after {timeout} ms"
            //TODO: Implement timeout for test methods
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                var method = FindTestMethod(test.TestName);
                var result = method.Invoke(null, null);

                if (result is Task task)
                {
                    await task;
                }

                test.PassedCases++;
                test.Status = TestStatus.Passed; 
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException tie && tie.InnerException != null)
                {
                    // Unwrap the inner exception to get the actual error
                    ex = tie.InnerException;
                }

                test.Status = TestStatus.Failed;
                test.ErrorMessage = ex.Message + "\n" + ex.StackTrace;
            }
            finally
            {
                stopwatch.Stop();
                test.Duration = stopwatch.ElapsedMilliseconds;
            }
        }

        private static async Task RunTestCaseAsync(TestResult test, CaseResult testCase)
        {
            // var timeout = GetCaseTimeout(test, testCase);
            //TODO: Implement timeout for test cases
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                testCase.Status = TestStatus.Running;

                var method = FindTestMethod(test.TestName);
                var parameters = ParseParameters(method.GetParameters(), testCase.Parameters);
                var result = method.Invoke(null, parameters);

                if (result is Task task)
                {
                    await task; // Await the async test method
                }

                testCase.Status = TestStatus.Passed;
                test.PassedCases++;
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException tie && tie.InnerException != null)
                {
                    // Unwrap the inner exception to get the actual error
                    ex = tie.InnerException;
                }

                testCase.Status = TestStatus.Failed;
                testCase.ErrorMessage = ex.Message + "\n" + ex.StackTrace;
                test.Status = TestStatus.Failed;
            }
            finally
            {
                stopwatch.Stop();
                testCase.Duration = stopwatch.ElapsedMilliseconds;
            }
        }

        private static MethodInfo FindTestMethod(string testName)
        {
            _testAssembly = Assembly.GetEntryAssembly() 
            ?? throw new InvalidOperationException("Unable to load the entry assembly.");

            return _testAssembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .First(m => m.GetCustomAttribute<TestAttribute>()?.Name == testName);
        }

        private static object[] ParseParameters(ParameterInfo[] parameterInfos, string parameters)
        {
            // Split parameter string by commas
            var parameterValues = parameters.Split(',')
                .Select(p => p.Trim())
                .ToArray();

            // Ensure parameter count matches
            if (parameterInfos.Length != parameterValues.Length)
            {
                string error = $"Mismatch between method parameters ({parameterInfos.Length}) and provided arguments ({parameterValues.Length}).";
                error += $"\nParameters: {string.Join(", ", parameterInfos.Select(p => p.Name))}";
                error += $"\nArguments: {string.Join(", ", parameterValues)}";
                throw new TargetParameterCountException(error);
            }

            // Convert each parameter to the appropriate type
            return parameterInfos.Select((paramInfo, index) =>
            {
                var targetType = paramInfo.ParameterType;
                var value = parameterValues[index];

                // Handle nullable types
                if (Nullable.GetUnderlyingType(targetType) != null)
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                if (targetType == null)
                    throw new InvalidOperationException("targetType is null");

                // Direct conversion for basic types
                if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal))
                {
                    return Convert.ChangeType(value, targetType);
                }

                // Reflection-based resolution for complex types
                return ResolveComplexType(targetType, value);
            }).ToArray();
        }

        private static object ResolveComplexType(Type targetType, string name)
        {
            // Dynamically find the containing type (e.g., based on where the test method is defined)
            _testAssembly = Assembly.GetEntryAssembly() 
            ?? throw new InvalidOperationException("Unable to load the entry assembly.");

            var containingType = _testAssembly
                .GetTypes()
                .FirstOrDefault(t => t.GetMember(name, BindingFlags.Static | BindingFlags.Public).Length != 0) 
                ?? throw new InvalidOperationException($"No containing type found for '{name}'.");

            // Look for a matching static field or property in the containing class
            var member = containingType
                .GetMember(name, BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) 
                ?? throw new InvalidOperationException($"No static field or property found with the name '{name}' for type '{targetType.Name}'.");

            // Resolve the value from the field or property
            return member switch
            {
                FieldInfo field => field.GetValue(null)
                    ?? throw new InvalidOperationException($"No value found for static field '{name}' in type '{containingType.Name}'."),
                PropertyInfo property => property.GetValue(null)
                    ?? throw new InvalidOperationException($"No value found for static property '{name}' in type '{containingType.Name}'."),
                _ => throw new InvalidOperationException($"Unsupported member type for '{name}' in type '{containingType.Name}'.")
            };
        }


        private static void CollapsePassedCases(TestResult test)
        {
            lock (_testResults)
            {
                // Remove case details for passed tests
                if (test.Cases.All(c => c.Status == TestStatus.Passed))
                {
                    test.Status = TestStatus.Passed;
                }
            }
        }

        private static int GetTestTimeout(TestResult test)
        {
            var method = FindTestMethod(test.TestName);
            var attr = method.GetCustomAttribute<TestAttribute>();
            return attr?.Timeout ?? 0; // Default to 0 (no timeout)
        }

        private static int GetCaseTimeout(TestResult test, CaseResult testCase)
        {
            var method = FindTestMethod(test.TestName);
            var attrs = method.GetCustomAttributes<CaseAttribute>();
            var caseAttr = attrs.FirstOrDefault(c => c.Name == testCase.CaseName);
            return caseAttr?.Timeout ?? 0; // Default to 0 (no timeout)
        }


        private static int _consoleTop = -1;
        private static void DisplayResults()
        {
            if (_consoleTop == -1)
            {
                _consoleTop = Console.CursorTop;
            }
            int top = Console.CursorTop;

            Console.SetCursorPosition(0, _consoleTop);
            Console.CursorVisible = false;
            // Clear previous output
            for (int i = _consoleTop; i < top; i++)
            {
                Console.WriteLine(new string(' ', Console.WindowWidth));
            }

            Console.SetCursorPosition(0, _consoleTop);
            Console.CursorVisible = false;

            int totalCases = _testResults.Sum(t => t.TotalCases);
            int passedCases = _testResults.Sum(t => t.PassedCases);

            long totalDuration = _testResults.Sum(t => t.Duration);

            Console.WriteLine($"Total - [{passedCases}/{totalCases} - {totalDuration}ms]");

            foreach (var test in _testResults)
            {
                var symbol = test.Status switch
                {
                    TestStatus.Pending => "⚬",
                    TestStatus.Running => "◌",
                    TestStatus.Passed => "✅",
                    TestStatus.Failed => "❌",
                    _ => "???"
                };

                string durationTest = test.Duration == 0 ? "" : $" [{test.Duration}ms]";
                string cases = test.Cases.Count == 0 ? "" : $" [{test.PassedCases}/{test.TotalCases}]";

                Console.Write($"{symbol} {test.TestName}{cases}{durationTest}");
                if (test.Status == TestStatus.Failed && test.Cases.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($" // {test.ErrorMessage?.Split('\n')[0]}");
                    Console.ResetColor();
                }
                Console.WriteLine();

                if (test.Status == TestStatus.Passed || test.Status == TestStatus.Pending) {
                    continue;
                }

                if (test.Cases.Count > 0)
                {
                    foreach (var testCase in test.Cases)
                    {
                        var caseSymbol = testCase.Status switch
                        {
                            TestStatus.Pending => "⚬",
                            TestStatus.Running => "◌",
                            TestStatus.Passed => "✅",
                            TestStatus.Failed => "❌",
                            _ => "???"
                        };

                        if (!test.Cases.Any(c => c.Status == TestStatus.Running) && (testCase.Status == TestStatus.Passed)) {
                            continue;
                        }

                        string durationCase = testCase.Duration == 0 ? "" : $" [{testCase.Duration}ms]";

                        Console.Write($"\t{caseSymbol} {testCase.CaseName}({testCase.ParametersString}){durationCase}");

                        if (testCase.Status == TestStatus.Failed)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write($" // {testCase.ErrorMessage?.Split('\n')[0]}");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    
        private static bool selectingTest = true;
        private static int selectedTest = 0;
        private static int selectedCase = 0;
        private static readonly List<(bool expanded, List<bool> casesExpanded)> testCasesDisplayStyle = [];

        private static void DisplayInteractiveResults() {
            
            int totalCases = _testResults.Sum(t => t.TotalCases);
            int passedCases = _testResults.Sum(t => t.PassedCases);
            long totalDuration = _testResults.Sum(t => t.Duration);
            Console.WriteLine($"Total - [{passedCases}/{totalCases} - {totalDuration}ms]");
            

            for (int i = 0; i < _testResults.Count; i++) {
                testCasesDisplayStyle.Add((false, new List<bool>(_testResults[i].Cases.Count)));
                for (int j = 0; j < _testResults[i].Cases.Count; j++) {
                    testCasesDisplayStyle[i].casesExpanded.Add(false);
                }
            }

            bool initialDisplay = true;
            while (true) {
                if (Console.KeyAvailable || initialDisplay) {
                    if (initialDisplay){
                        initialDisplay = false;
                    }
                    else {
                        if (HandleInput()) {
                            break;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(20);
                    continue;
                }

                int top = Console.CursorTop;
                Console.SetCursorPosition(0, _consoleTop + 1);
                Console.CursorVisible = false;
                for (int i = _consoleTop + 1; i < top; i++) {
                    Console.WriteLine(new string(' ', Console.WindowWidth));
                }
                Console.SetCursorPosition(0, _consoleTop + 1);
                Console.CursorVisible = false;

                for (int i = 0; i < _testResults.Count; i++)
                {
                    string selected = i == selectedTest ? "> " : "  ";

                    TestResult? test = _testResults[i];
                    var symbol = test.Status switch
                    {
                        TestStatus.Pending => "⚬",
                        TestStatus.Running => "◌",
                        TestStatus.Passed => "✅",
                        TestStatus.Failed => "❌",
                        _ => "???"
                    };

                    string durationTest = test.Duration == 0 ? "" : $"[{test.Duration}ms]";
                    string cases = test.Cases.Count == 0 ? "" : $"[{test.PassedCases}/{test.TotalCases}]";

                    Console.Write($"{selected}{symbol} {test.TestName} {cases} {durationTest}");
                    if (test.Status == TestStatus.Failed && test.Cases.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (!testCasesDisplayStyle[i].expanded)
                        {
                            Console.Write($" // {test.ErrorMessage?.Split('\n')[0]}");
                        }
                        else
                        {
                            Console.Write($"\n    {test.ErrorMessage}");
                        }
                        Console.ResetColor();
                    }
                    Console.WriteLine();

                    if (test.Cases.Count > 0 && (testCasesDisplayStyle[i].expanded || (selectedTest == i && !selectingTest)))
                    {
                        for (int i1 = 0; i1 < test.Cases.Count; i1++)
                        {
                            selected = i == selectedTest && i1 == selectedCase && !selectingTest ? "> " : "  ";

                            CaseResult? testCase = test.Cases[i1];
                            var caseSymbol = testCase.Status switch
                            {
                                TestStatus.Pending => "◌",
                                TestStatus.Passed => "✅",
                                TestStatus.Failed => "❌",
                                _ => "???"
                            };

                            string durationCase = testCase.Duration == 0 ? "" : $"[{testCase.Duration}ms]";

                            if (!testCasesDisplayStyle[i].casesExpanded[i1])
                                Console.Write($"  {selected}{caseSymbol} {testCase.CaseName}({string.Join(", ", testCase.ParametersString)}) {durationCase}");
                            else
                                Console.Write($"  {selected}{caseSymbol} {testCase.CaseName}(\n{string.Join(",\n", testCase.ParametersString)}\n) {durationCase}");

                            if (testCase.Status == TestStatus.Failed)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                testCase.ErrorMessage ??= "No error message provided.";
                                if (!testCasesDisplayStyle[i].casesExpanded[i1])
                                {
                                    Console.Write($" // {testCase.ErrorMessage.Split('\n')[0]}");
                                }
                                else {
                                    Console.Write($"\n      {string.Join("\n      ", testCase.ErrorMessage.Split('\n'))}");
                                }
                                Console.ResetColor();
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }
        }
    
        private static bool HandleInput() {
            var key = Console.ReadKey(true);
            if (key.KeyChar == 'q') {
                return true;
            }
            if (key.Key == ConsoleKey.UpArrow) {
                if (selectingTest) {
                    selectedTest--;
                } else {
                    selectedCase--;
                }
            }
            if (key.Key == ConsoleKey.DownArrow) {
                if (selectingTest) {
                    selectedTest++;
                } else {
                    selectedCase++;
                }
            }
            selectedTest = Math.Max(0, Math.Min(selectedTest, _testResults.Count - 1));
            selectedCase = Math.Max(0, Math.Min(selectedCase, _testResults[selectedTest].Cases.Count - 1));
            if (key.Key == ConsoleKey.LeftArrow) {
                selectedCase = 0;
            }
            if (key.Key == ConsoleKey.LeftArrow) {
                selectingTest = true;
            }
            if (key.Key == ConsoleKey.RightArrow) {
                if (_testResults[selectedTest].Cases.Count > 0) {
                    selectingTest = false;
                }
            }
            if (key.Key == ConsoleKey.Enter) {
                if (selectingTest) {
                    var styleInfo = testCasesDisplayStyle[selectedTest];
                    styleInfo.expanded = !styleInfo.expanded;
                    testCasesDisplayStyle[selectedTest] = styleInfo;
                } else {
                    testCasesDisplayStyle[selectedTest].casesExpanded[selectedCase] = !testCasesDisplayStyle[selectedTest].casesExpanded[selectedCase];
                }
            }
            return false;
        }
    }
}