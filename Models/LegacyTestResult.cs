namespace Apify.Models
{
    public class LegacyTestResult
    {
        public string TestName { get; set; } = string.Empty;
        
        // Adding Name as an alias for TestName to maintain compatibility
        public string Name => TestName;
        
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ActualValue { get; set; }
        public string? ExpectedValue { get; set; }
        
        // For compatibility with existing code
        public bool Passed
        {
            get => Success;
            set => Success = value;
        }

        public static LegacyTestResult CreateSuccess(string testName)
        {
            return new LegacyTestResult
            {
                TestName = testName,
                Success = true
            };
        }

        public static LegacyTestResult Failure(string testName, string errorMessage, string? actual = null, string? expected = null)
        {
            return new LegacyTestResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = errorMessage,
                ActualValue = actual,
                ExpectedValue = expected
            };
        }
    }
}
