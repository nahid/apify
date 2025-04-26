using System.Text.Json.Serialization;

namespace APITester.Models
{
    public class TestAssertion
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("assertion")]
        public string Assertion { get; set; } = string.Empty;

        [JsonPropertyName("expected")]
        public object? Expected { get; set; }

        // Helper properties to understand the assertion type
        public AssertionType GetAssertionType()
        {
            if (Assertion.StartsWith("status"))
                return AssertionType.StatusCode;
            if (Assertion.StartsWith("response.body"))
                return AssertionType.ResponseBody;
            if (Assertion.StartsWith("response.headers"))
                return AssertionType.ResponseHeader;
            if (Assertion.StartsWith("response.time"))
                return AssertionType.ResponseTime;
            
            return AssertionType.Unknown;
        }

        public string GetComparisonType()
        {
            if (Assertion.Contains("=="))
                return "equals";
            if (Assertion.Contains("!="))
                return "notEquals";
            if (Assertion.Contains("contains"))
                return "contains";
            if (Assertion.Contains("startsWith"))
                return "startsWith";
            if (Assertion.Contains("endsWith"))
                return "endsWith";
            if (Assertion.Contains("<"))
                return "lessThan";
            if (Assertion.Contains(">"))
                return "greaterThan";
            if (Assertion.Contains("<="))
                return "lessThanOrEqual";
            if (Assertion.Contains(">="))
                return "greaterThanOrEqual";
            if (Assertion.Contains("match"))
                return "matches";

            return "unknown";
        }
    }

    public enum AssertionType
    {
        StatusCode,
        ResponseBody,
        ResponseHeader,
        ResponseTime,
        Unknown
    }
}
