namespace MarcoZechner.JTest; 

public record TestResult
{
    public AssertException? AssertException { get; set; }
    public string? FailMessage { get; set; }
    public long Duration { get; set; }
}