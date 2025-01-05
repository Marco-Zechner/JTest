namespace MarcoZechner.JTest {
    public class TestResult
    {
        public required string TestName { get; set; }
        public TestStatus Status { get; set; }
        public int TotalCases { get; set; }
        public int PassedCases { get; set; }
        public string? ErrorMessage { get; set; }
        public List<CaseResult> Cases { get; set; } = [];
        public long Duration { get; set; }
    }
}