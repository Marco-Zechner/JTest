namespace MarcoZechner.JTest; 

public record TestResult
{
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public long Duration { get; set; }
}