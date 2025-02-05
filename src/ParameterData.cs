namespace MarcoZechner.JTest; 

public record ParameterData{
    public required Type ParameterType { get; init; }
    public required string ParameterName { get; init; }
    public required object? ParameterValue { get; init; }
} 