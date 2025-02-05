namespace MarcoZechner.JTest; 

public record TestCase{
    public bool IsDefault { get; init; } = false;
    public string? CaseName { get; init; }
    public required string TestName { get; init; }
    public List<ParameterData> Parameters { get; init; } = [];
    public Status Status { get; set; } = Status.NotRun;
    public TestResult? Result { get; set; }

    // Metadata
    public required string NamespaceName { get; init; }
    public required string ClassName { get; init; }
    public required string MethodName { get; init; }
    public required string FullCategoryPath { get; init; }
}