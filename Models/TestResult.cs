namespace Apify.Models
{
    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ActualValue { get; set; }
        public string? ExpectedValue { get; set; }

        public static TestResult Success(string testName)
        {
            return new TestResult
            {
                TestName = testName,
                Passed = true
            };
        }

        public static TestResult Failure(string testName, string errorMessage, string? actual = null, string? expected = null)
        {
            return new TestResult
            {
                TestName = testName,
                Passed = false,
                ErrorMessage = errorMessage,
                ActualValue = actual,
                ExpectedValue = expected
            };
        }
    }
}
