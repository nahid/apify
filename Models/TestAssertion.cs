using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields | 
                               DynamicallyAccessedMemberTypes.PublicMethods |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class TestAssertion
    {
        [Newtonsoft.Json.JsonConstructor]
        public TestAssertion()
        {
            Name = string.Empty;
            Description = string.Empty;
            Assertion = string.Empty;
            AssertType = string.Empty;
        }
        
        // Make sure name and description are synchronized
        public void SynchronizeProperties()
        {
            // Make sure Name is populated from either Description or Name
            if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Description))
            {
                Name = Description;
            }
            else if (string.IsNullOrEmpty(Description) && !string.IsNullOrEmpty(Name))
            {
                Description = Name;
            }
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("assertion")]
        public string Assertion { get; set; } = string.Empty;
        
        [JsonPropertyName("AssertType")]
        public string AssertType { get; set; } = string.Empty;
        
        [JsonPropertyName("Property")]
        public string? Property { get; set; }
        
        [JsonPropertyName("ExpectedValue")]
        public string? ExpectedValue { get; set; }

        [JsonPropertyName("expected")]
        public object? Expected { get; set; }

        // Helper properties to understand the assertion type
        public AssertionType GetAssertionType()
        {
            // Support both formats
            if (!string.IsNullOrEmpty(AssertType))
            {
                // New format using AssertType property
                switch (AssertType.ToLowerInvariant())
                {
                    case "statuscode":
                        return AssertionType.StatusCode;
                    case "containsproperty":
                        return AssertionType.ResponseBody;
                    case "headercontains":
                        return AssertionType.ResponseHeader;
                    case "responsetimebelow":
                        return AssertionType.ResponseTime;
                    default:
                        return AssertionType.Unknown;
                }
            }
            
            // Legacy format using Assertion string
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
            // Support both formats
            if (!string.IsNullOrEmpty(AssertType))
            {
                // New format using AssertType property
                switch (AssertType.ToLowerInvariant())
                {
                    case "statuscode":
                        return "equals";
                    case "containsproperty":
                        return "contains";
                    case "headercontains":
                        return "contains";
                    case "responsetimebelow":
                        return "lessThan";
                    default:
                        return "unknown";
                }
            }
            
            // Legacy format using Assertion string
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
        
        // This is handled in the constructor now
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
