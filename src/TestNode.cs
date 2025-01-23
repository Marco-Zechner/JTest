namespace MarcoZechner.JTest; 

public record TestNode{
    public required string Name { get; init; }
    public List<TestCase> TestCases { get; init; } = [];
    public List<TestNode> TestCasesHolder { get; init; } = [];
    public List<TestNode> Children { get; init; } = [];
}