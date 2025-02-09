using MarcoZechner.PrettyReflector;
using MarcoZechner.ConsoleBox;


namespace MarcoZechner.JTest; 

public class CodeRunner(List<TestNode> rootNodesForViews)
{
    private readonly List<TestNode> codeView = rootNodesForViews;

    private static DisplayPane testSelector = new() {
        PanelName = "Test Selector",
        Content = "",
        RelativeSize = 0.4f,
        Truncate = true
    };

    private static SplitPane testView = new() {
        Orientation = Orientation.Vertical,
        PanelName = "Test View",
        RelativeSize = 0.6f
    };

    private static SplitPane variableView = new() {
        Orientation = Orientation.Horizontal,
        PanelName = "Variable View",
        RelativeSize = 0.3f
    };

    private static DisplayPane testResultView = new() {
        PanelName = "Test Result View",
        Content = "",
        RelativeSize = 0.7f,
        Truncate = false
    };

    private static DisplayPane variableNameView = new() {
        PanelName = "Variable Result View",
        Content = "",
        RelativeSize = 0.3f,
        Truncate = true
    };

    private static DisplayPane variableValueView = new() {
        PanelName = "Variable Result View",
        Content = "",
        RelativeSize = 0.7f,
        Truncate = true
    };

    private static SplitPane layer1 = new() {
        Orientation = Orientation.Horizontal,
        PanelName = "Layer 1",
    };

    private PanelManager main = new();

    private int viewNodeIndex = 0;
    private List<int> selectedIndices = [0];

    private void HandleInput(ConsoleKeyInfo input)
    {
        if (input.Key == ConsoleKey.Escape)
            main.Stop();

        if (input.Key == ConsoleKey.Tab)
        {
            viewNodeIndex = (viewNodeIndex + 1) % codeView.Count;
            selectedIndices = [0];
        }

        if (input.Key == ConsoleKey.LeftArrow) {
            if (input.Modifiers.HasFlag(ConsoleModifiers.Control))
                testSelector.HorizontalOffset--;
            else if (selectedIndices.Count > 1)
                selectedIndices.RemoveAt(selectedIndices.Count - 1);
        }

        if (input.Key == ConsoleKey.RightArrow) {
            if (input.Modifiers.HasFlag(ConsoleModifiers.Control))
                testSelector.HorizontalOffset++;
            else if (GetSelectedNode(selectedIndices, codeView[viewNodeIndex]).selectedCase == null)
                selectedIndices.Add(0); 
        }

        if (input.Key == ConsoleKey.UpArrow) {
            if (selectedIndices.Count > 0 && selectedIndices[^1] > 0)
                selectedIndices[^1]--;
        }

        if (input.Key == ConsoleKey.DownArrow) {
            var selection = GetSelectedNode([.. selectedIndices.SkipLast(1)], codeView[viewNodeIndex]);
            int maxIndex = selection.selectedNode?.TestCases.Count + selection.selectedNode?.TestCasesHolder.Count + selection.selectedNode?.Children.Count ?? 0;
            if (selectedIndices.Count > 0 && selectedIndices[^1] < maxIndex - 1)
                selectedIndices[^1]++;
        }

        if (input.Key == ConsoleKey.Enter) {
            var (selectedNode, selectedCase) = GetSelectedNode(selectedIndices, codeView[viewNodeIndex]);
            if (selectedCase != null)
            {
                testRunner = TestManager.RunTestsAsync([selectedCase]);
                Task.Run(async () => {
                    await testRunner;
                });
            }
        }
    }

    private Task<List<TestCase>>? testRunner = null;

    private Task BeforeRender(PanelBase root, RenderBuffer current) {
        testSelector.Content = Render(codeView[viewNodeIndex], selectedIndices);
        var (selectedNode, selectedCase) = GetSelectedNode(selectedIndices, codeView[viewNodeIndex]);
        testSelector.Content += selectedCase == null ? (selectedNode?.Name ?? "No node selected") : selectedCase.TestName;
        testSelector.Content += string.Join(" -> ", selectedIndices);

        if (testRunner != null)
        {
            if (testRunner.IsCompleted) {
                var result = testRunner.Result;

                variableNameView.Content = string.Join("\n", result.SelectMany(testCase => testCase.Parameters.Select(parameter => parameter.ParameterName)));
                variableValueView.Content = string.Join("\n", result.SelectMany(testCase => testCase.Parameters.Select(parameter => parameter.ParameterValue.PrettyValue())));

                testResultView.Content = string.Join("\n", result.Select(testCase => $"{testCase.TestName} = {testCase.Status}\n\n{testCase.Result}"));
                testRunner = null;
            }
        }

        return Task.CompletedTask;
    }

    public async Task Render() {
        main.RootPanel = layer1;
        layer1.Panels.Add(testSelector);
        layer1.AddSeperator();
        layer1.Panels.Add(testView);
        testView.Panels.Add(variableView);
        testView.AddSeperator();
        testView.Panels.Add(testResultView);
        variableView.Panels.Add(variableNameView);
        variableView.AddSeperator();
        variableView.Panels.Add(variableValueView);

        main.HandleInputMethod = HandleInput;
        main.BeforeRender = BeforeRender;    

        main.Start();
    }

    private static (TestNode? selectedNode, TestCase? selectedCase) GetSelectedNode(List<int> selectedIndices, TestNode rootNode) {
        TestNode? selectedNode = rootNode;
        foreach (var index in selectedIndices) {
            int relativeIndex = index;
            if (relativeIndex < selectedNode.TestCases.Count) {
                return (selectedNode, selectedNode.TestCases[relativeIndex]);
            }
            relativeIndex -= selectedNode.TestCases.Count;
            if (relativeIndex < selectedNode.TestCasesHolder.Count) {
                selectedNode = selectedNode.TestCasesHolder[relativeIndex];
                continue;
            }
            relativeIndex -= selectedNode.TestCasesHolder.Count;
            if (relativeIndex < selectedNode.Children.Count) {
                selectedNode = selectedNode.Children[relativeIndex];
                continue;
            }
            throw new InvalidOperationException("Invalid selection index");
        }
        return (selectedNode, null);
    }

    private static string Render(TestNode node, List<int> selectedIndices, string prefix = "", string firstPrefix = "")
    {
        string output = firstPrefix + node.Name + "\n";

        bool thickVer = node.Children.Count + node.TestCasesHolder.Count > 0;

        output += RenderTestCases(node, selectedIndices, prefix, thickVer);
        output += RenderTestCaseHolders(node, selectedIndices, prefix, thickVer);
        output += RenderChildNodes(node, selectedIndices, prefix, thickVer);
        return output;
    }

    private static string RenderTestCases(TestNode node, List<int> selectedIndices, string prefix, bool thickVer)
    {
        string output = "";
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        for (int i = 0; i < node.TestCases.Count; i++)
        {
            TestCase testCase = node.TestCases[i];
            bool isLast = (i == node.TestCases.Count - 1) && node.TestCasesHolder.Count == 0 && node.Children.Count == 0;
            string nextFirstPrefix = $"{prefix}{GetCross(isLast, thickVer, false, 1, i == selectedIndex)} ";
            string parametersString = FormatParameters(testCase.Parameters);
            output += $"{nextFirstPrefix}{testCase.CaseName ?? testCase.TestName}{parametersString}\n";
        }
        return output;
    }

    private static string RenderTestCaseHolders(TestNode node, List<int> selectedIndices, string prefix, bool thickVer)
    {
        string output = "";
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        selectedIndex -= node.TestCases.Count;
        for (int i = 0; i < node.TestCasesHolder.Count; i++)
        {
            bool selected = i == selectedIndex;

            TestNode testCaseHolder = node.TestCasesHolder[i];
            bool isLastHolder = (i == node.TestCasesHolder.Count - 1) && node.Children.Count == 0;
            string nextFirstPrefix = $"{prefix}{GetCross(isLastHolder, thickVer, true, 1, selected)} ";

            output += $"{nextFirstPrefix}{testCaseHolder.Name}\n";
            output += RenderTestCaseHolder(testCaseHolder, selected ? [.. selectedIndices.Skip(1)] : [], prefix, thickVer, isLastHolder);
        }
        return output;
    }

    private static string RenderChildNodes(TestNode node, List<int> selectedIndices, string prefix, bool thickVer)
    {
        string output = "";
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        selectedIndex -= node.TestCases.Count + node.TestCasesHolder.Count;
        for (int i = 0; i < node.Children.Count; i++)
        {
            bool selected = i == selectedIndex;
            TestNode child = node.Children[i];
            bool isLast = i == node.Children.Count - 1;
            string nextPrefix = $"{prefix}{GetLine(isLast, thickVer)} ";
            string nextFirstPrefix = $"{prefix}{GetCross(isLast, thickVer, true, 1, selected)} ";
            output += Render(child, selected ? [.. selectedIndices.Skip(1)] : [], nextPrefix, nextFirstPrefix);
        }
        return output;
    }

    private static string GetLine(bool isLast, bool thickVer, int extend = 1)
    {
        char verticalChar = thickVer ? '║' : '│';
        return (isLast ? " " : verticalChar.ToString()) + new string(' ', extend);
    }

    private static string GetCross(bool isLast, bool thickVer, bool thickHor, int extend = 1, bool selected = false)
    {
        if (isLast && thickVer != thickHor)
            throw new InvalidOperationException("Invalid configuration: isLast && thickVer != thickHor");
        if (!thickVer && thickHor)
            throw new InvalidOperationException("Invalid configuration: !thickVer && thickHor");

        char cornerChar = isLast
            ? (thickVer ? '╚' : '└')
            : (thickVer ? (thickHor ? '╠' : '╟') : '├');
        char horizontalChar = thickHor ? '═' : '─';
        if (selected)
            return cornerChar + new string(horizontalChar, extend-1) + ">";
        return cornerChar + new string(horizontalChar, extend);
    }

    private static string GetTestCaseSuffix(TestCase testCase, List<TestCase> allTestCases)
    {
        if (allTestCases.Any(
            otherTestCase => testCase != otherTestCase &&
            testCase.MethodName == otherTestCase.MethodName &&
            testCase.FullCategoryPath == otherTestCase.FullCategoryPath &&
            testCase.ClassName != otherTestCase.ClassName))
        {
            return $" [{testCase.ClassName}]";
        }

        if (allTestCases.Any(
            otherTestCase => testCase != otherTestCase &&
            testCase.MethodName == otherTestCase.MethodName &&
            testCase.FullCategoryPath == otherTestCase.FullCategoryPath &&
            testCase.ClassName == otherTestCase.ClassName &&
            testCase.NamespaceName != otherTestCase.NamespaceName))
        {
            return $" [{testCase.NamespaceName}.{testCase.ClassName}]";
        }

        return string.Empty;
    }

    private static string FormatParameters(IEnumerable<ParameterData> parameters)
    {
        if (parameters == null || !parameters.Any())
            return string.Empty;

        string formatted = string.Join(", ", 
            parameters.Select(parameterData => 
                Prettify.Variable(parameterData.ParameterType, parameterData.ParameterName, parameterData.ParameterValue)));
        formatted = formatted.Replace("\n", "\\n");

        return string.IsNullOrEmpty(formatted) ? string.Empty : $" ({formatted})";
    }

    private static string RenderTestCaseHolder(TestNode testCaseHolder, List<int> selectedIndices, string prefix, bool thickVer, bool isLastHolder)
    {
        string output = "";
        string nextPrefix = $"{prefix}{GetLine(isLastHolder, thickVer)} ";
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        int indexCounter = 0;
        foreach (var (testCase, isLastInner) in EnumerateWithLastFlag(testCaseHolder.TestCases))
        {
            string innerPrefix = $"{nextPrefix}{GetCross(isLastInner, false, false, 1, indexCounter == selectedIndex)} ";
            string suffix = GetTestCaseSuffix(testCase, testCaseHolder.TestCases);
            string parametersString = FormatParameters(testCase.Parameters);
            output += $"{innerPrefix}{testCase.CaseName ?? testCase.TestName}{parametersString}{suffix}\n";
            indexCounter++;
        }
        return output;
    }

    private static IEnumerable<(T Item, bool IsLast)> EnumerateWithLastFlag<T>(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        for (int i = 0; i < itemList.Count; i++)
        {
            yield return (itemList[i], i == itemList.Count - 1);
        }
    }
}