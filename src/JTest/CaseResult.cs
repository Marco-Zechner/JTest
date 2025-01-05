namespace MarcoZechner.JTest {

    public class CaseResult
    {
        public required string CaseName { get; set; }
        public required string Parameters { get; set; }
        public required string ParametersString { get; set; }
        public TestStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public long Duration { get; set; }
        
    } 
}