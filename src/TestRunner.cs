using MarcoZechner.PrettyReflector;

namespace MarcoZechner.JTest; 

public class CodeRunner(List<TestNode> rootNodesForViews)
{
    private readonly List<TestNode> codeView = rootNodesForViews;
    public async Task Render() {
        int viewNodeIndex = 0;
        List<int> selectedIndices = [0];
        while (true) {
            ConsoleKeyInfo? input = null;
            if (Console.KeyAvailable)
                input = Console.ReadKey(true);
            
            if (input?.Key == ConsoleKey.Escape)
                break;

            if (input?.Key == ConsoleKey.Tab)
            {
                viewNodeIndex = (viewNodeIndex + 1) % codeView.Count;
                selectedIndices = [0];
            }

            if (input?.Key == ConsoleKey.LeftArrow) {
                if (selectedIndices.Count > 1)
                    selectedIndices.RemoveAt(selectedIndices.Count - 1);
            }

            if (input?.Key == ConsoleKey.RightArrow) {
                if (GetSelectedNode(selectedIndices, codeView[viewNodeIndex]).selectedCase == null)
                    selectedIndices.Add(0); 
            }

            if (input?.Key == ConsoleKey.UpArrow) {
                if (selectedIndices.Count > 0 && selectedIndices[^1] > 0)
                    selectedIndices[^1]--;
            }

            if (input?.Key == ConsoleKey.DownArrow) {
                var selection = GetSelectedNode([.. selectedIndices.SkipLast(1)], codeView[viewNodeIndex]);
                int maxIndex = selection.selectedNode?.TestCases.Count + selection.selectedNode?.TestCasesHolder.Count + selection.selectedNode?.Children.Count ?? 0;
                if (selectedIndices.Count > 0 && selectedIndices[^1] < maxIndex - 1)
                    selectedIndices[^1]++;
            }

            Render(codeView[viewNodeIndex], selectedIndices);
            var (selectedNode, selectedCase) = GetSelectedNode(selectedIndices, codeView[viewNodeIndex]);
            Console.WriteLine(selectedCase == null ? (selectedNode?.Name ?? "No node selected") : selectedCase.TestName);
            Console.WriteLine(string.Join(" -> ", selectedIndices));
            await Task.Delay(100);
            Console.Clear();
        }
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

    private static void Render(TestNode node, List<int> selectedIndices, string prefix = "", string firstPrefix = "")
    {
        Console.WriteLine($"{firstPrefix}{node.Name}");

        bool thickVer = node.Children.Count + node.TestCasesHolder.Count > 0;

        RenderTestCases(node, selectedIndices, prefix, thickVer);
        RenderTestCaseHolders(node, selectedIndices, prefix, thickVer);
        RenderChildNodes(node, selectedIndices, prefix, thickVer);
    }


    private static void RenderTestCases(TestNode node, List<int> selectedIndices, string prefix, bool thickVer)
    {
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        for (int i = 0; i < node.TestCases.Count; i++)
        {
            TestCase testCase = node.TestCases[i];
            bool isLast = (i == node.TestCases.Count - 1) && node.TestCasesHolder.Count == 0 && node.Children.Count == 0;
            string nextFirstPrefix = $"{prefix}{GetCross(isLast, thickVer, false, 1, i == selectedIndex)} ";
            string parametersString = FormatParameters(testCase.Parameters);
            Console.WriteLine($"{nextFirstPrefix}{testCase.CaseName ?? testCase.TestName}{parametersString}");
        }
    }

    private static void RenderTestCaseHolders(TestNode node, List<int> selectedIndices, string prefix, bool thickVer)
    {
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        selectedIndex -= node.TestCases.Count;
        for (int i = 0; i < node.TestCasesHolder.Count; i++)
        {
            bool selected = i == selectedIndex;

            TestNode testCaseHolder = node.TestCasesHolder[i];
            bool isLastHolder = (i == node.TestCasesHolder.Count - 1) && node.Children.Count == 0;
            string nextFirstPrefix = $"{prefix}{GetCross(isLastHolder, thickVer, true, 1, selected)} ";

            Console.WriteLine($"{nextFirstPrefix}{testCaseHolder.Name}");
            RenderTestCaseHolder(testCaseHolder, selected ? [.. selectedIndices.Skip(1)] : [], prefix, thickVer, isLastHolder);
        }
    }

    private static void RenderChildNodes(TestNode node, List<int> selectedIndices, string prefix, bool thickVer)
    {
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        selectedIndex -= node.TestCases.Count + node.TestCasesHolder.Count;
        for (int i = 0; i < node.Children.Count; i++)
        {
            bool selected = i == selectedIndex;
            TestNode child = node.Children[i];
            bool isLast = i == node.Children.Count - 1;
            string nextPrefix = $"{prefix}{GetLine(isLast, thickVer)} ";
            string nextFirstPrefix = $"{prefix}{GetCross(isLast, thickVer, true, 1, selected)} ";
            Render(child, selected ? [.. selectedIndices.Skip(1)] : [], nextPrefix, nextFirstPrefix);
        }
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

    private static void RenderTestCaseHolder(TestNode testCaseHolder, List<int> selectedIndices, string prefix, bool thickVer, bool isLastHolder)
    {
        string nextPrefix = $"{prefix}{GetLine(isLastHolder, thickVer)} ";
        int selectedIndex = selectedIndices.Count > 0 ? selectedIndices[0] : -1;
        int indexCounter = 0;
        foreach (var (testCase, isLastInner) in EnumerateWithLastFlag(testCaseHolder.TestCases))
        {
            string innerPrefix = $"{nextPrefix}{GetCross(isLastInner, false, false, 1, indexCounter == selectedIndex)} ";
            string suffix = GetTestCaseSuffix(testCase, testCaseHolder.TestCases);
            string parametersString = FormatParameters(testCase.Parameters);
            Console.WriteLine($"{innerPrefix}{testCase.CaseName ?? testCase.TestName}{parametersString}{suffix}");
            indexCounter++;
        }
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