using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Apify.Utils;

namespace Apify.Models
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
        
        // This method should be called after deserialization to convert old format to new format
        public void ConvertLegacyFormat()
        {
            // If AssertType is already set, no need to convert
            if (!string.IsNullOrEmpty(AssertType))
                return;
                
            // Convert legacy format assertions to the new format
            if (!string.IsNullOrEmpty(Assertion))
            {
                string assertionLower = Assertion.ToLowerInvariant();
                
                // Status code assertions
                if (assertionLower.StartsWith("status") || assertionLower.StartsWith("response.status"))
                {
                    AssertType = "StatusCode";
                    // Extract expected status code if it's a simple equality check
                    if (assertionLower.Contains("=="))
                    {
                        int equalsPos = assertionLower.IndexOf("==");
                        if (equalsPos >= 0 && equalsPos + 2 < assertionLower.Length)
                        {
                            string statusValue = assertionLower.Substring(equalsPos + 2).Trim();
                            // Remove any quotes or spaces
                            statusValue = statusValue.Replace("'", "").Replace("\"", "").Trim();
                            ExpectedValue = statusValue;
                        }
                    }
                }
                // Response body contains property assertion
                else if (assertionLower.Contains("response.body") && assertionLower.Contains("contains"))
                {
                    AssertType = "ContainsProperty";
                    // Try to extract property name
                    int containsPos = assertionLower.IndexOf("contains");
                    if (containsPos >= 0 && containsPos + 8 < assertionLower.Length)
                    {
                        string propValue = assertionLower.Substring(containsPos + 8).Trim();
                        // Remove any quotes or parentheses
                        propValue = propValue.Replace("'", "").Replace("\"", "").Replace("(", "").Replace(")", "").Trim();
                        ExpectedValue = propValue;
                    }
                }
                // Response header contains value assertion
                else if (assertionLower.Contains("response.headers") && assertionLower.Contains("contains"))
                {
                    AssertType = "HeaderContains";
                    // Try to extract header name and expected value
                    int dotPos = assertionLower.IndexOf("headers.");
                    if (dotPos >= 0)
                    {
                        string headerPath = assertionLower.Substring(dotPos + 8);
                        int bracketPos = headerPath.IndexOf("[");
                        int closeBracketPos = headerPath.IndexOf("]");
                        if (bracketPos >= 0 && closeBracketPos > bracketPos)
                        {
                            string headerName = headerPath.Substring(bracketPos + 1, closeBracketPos - bracketPos - 1);
                            headerName = headerName.Replace("'", "").Replace("\"", "").Trim();
                            Property = headerName;
                            
                            // Try to extract expected value
                            int containsPos = headerPath.IndexOf("contains");
                            if (containsPos >= 0 && containsPos + 8 < headerPath.Length)
                            {
                                string expectedValue = headerPath.Substring(containsPos + 8).Trim();
                                expectedValue = expectedValue.Replace("'", "").Replace("\"", "").Replace("(", "").Replace(")", "").Trim();
                                ExpectedValue = expectedValue;
                            }
                        }
                    }
                }
                // Response time assertion
                else if (assertionLower.Contains("response.time") && (assertionLower.Contains("<") || assertionLower.Contains("lessthan")))
                {
                    AssertType = "ResponseTimeBelow";
                    // Try to extract max response time
                    int ltPos = assertionLower.IndexOf("<");
                    if (ltPos < 0) ltPos = assertionLower.IndexOf("lessthan");
                    if (ltPos >= 0 && ltPos + 1 < assertionLower.Length)
                    {
                        string timeValue = assertionLower.Substring(ltPos + 1).Trim();
                        // Remove any non-digit characters to get the number
                        timeValue = new string(timeValue.Where(char.IsDigit).ToArray());
                        ExpectedValue = timeValue;
                    }
                }
                // Equal assertion (property has specific value)
                else if (assertionLower.Contains("==") || assertionLower.Contains("equals"))
                {
                    AssertType = "Equal";
                    // Try to extract property path and expected value
                    int equalsPos = assertionLower.IndexOf("==");
                    if (equalsPos < 0) equalsPos = assertionLower.IndexOf("equals");
                    
                    if (equalsPos > 0)
                    {
                        string leftSide = assertionLower.Substring(0, equalsPos).Trim();
                        string rightSide = assertionLower.Substring(equalsPos + 2).Trim();
                        
                        // Remove common prefixes like "response.body." or "json."
                        leftSide = leftSide.Replace("response.body.", "").Replace("json.", "").Trim();
                        
                        // Set property path
                        PropertyPath = leftSide;
                        
                        // Extract expected value, removing quotes
                        rightSide = rightSide.Replace("'", "").Replace("\"", "").Trim();
                        ExpectedValue = rightSide;
                    }
                }
            }
            
            // Synchronize properties to ensure name/description are populated
            SynchronizeProperties();
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
        
        [JsonPropertyName("propertyPath")]
        [Newtonsoft.Json.JsonProperty("propertyPath")]
        public string? PropertyPath { get; set; }
        
        [JsonPropertyName("ExpectedValue")]
        public string? ExpectedValue { get; set; }

        [JsonPropertyName("expected")]
        public object? Expected { get; set; }

        // Helper properties to understand the assertion type
        public AssertionType GetAssertionType()
        {
            // Always convert legacy format first
            ConvertLegacyFormat();
            
            // Support all possible formats
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
                    case "equal":
                        return AssertionType.ResponseBody;
                    default:
                        return AssertionType.Unknown;
                }
            }
            
            // Support property "type" from test files
            if (!string.IsNullOrEmpty(Property) && Property == "type")
            {
                return AssertionType.Unknown;  // Will handle later
            }
            
            // Legacy format using Assertion string
            if (!string.IsNullOrEmpty(Assertion))
            {
                if (Assertion.StartsWith("status"))
                    return AssertionType.StatusCode;
                if (Assertion.StartsWith("response.body"))
                    return AssertionType.ResponseBody;
                if (Assertion.StartsWith("response.headers"))
                    return AssertionType.ResponseHeader;
                if (Assertion.StartsWith("response.time"))
                    return AssertionType.ResponseTime;
            }
            
            return AssertionType.Unknown;
        }

        public string GetComparisonType()
        {
            // Always convert legacy format first
            ConvertLegacyFormat();
            
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
                    case "equal":
                        return "equals";
                    default:
                        return "unknown";
                }
            }
            
            // Legacy format using Assertion string
            if (!string.IsNullOrEmpty(Assertion))
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
            }

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
