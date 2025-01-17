using MarcoZechner.PrettyReflector;

namespace MarcoZechner.JTest; 

public class CodeRunner(TestNode CodeView, TestNode CategoryView)
{
    private readonly TestNode codeView = CodeView;
    private readonly TestNode categoryView = CategoryView;

    public async Task Render() {
        await Render(codeView, 0);
    }

    private static async Task Render(TestNode node, int indent) {
       Console.WriteLine(new string(' ', 2 * indent) + node.Name);
        foreach (var testCase in node.TestCases) {
            string caseString = $"{new string(' ', 2 * indent)}- {testCase.CaseName ?? testCase.TestName}";

            string parametersString = string.Join(", ", 
                testCase.Parameters.Select(parameterData => 
                    Prettify.Variable(parameterData.ParameterType, parameterData.ParameterName, parameterData.ParameterValue)));

            parametersString = parametersString.Replace("\n", "\\n"); 

            if (string.IsNullOrEmpty(parametersString)) {
                Console.WriteLine(caseString);
            } else
                Console.WriteLine(caseString + " (" + parametersString + ")");
        }
        foreach (var child in node.Children) {
            await Render(child, indent + 1);
        }
    }
}