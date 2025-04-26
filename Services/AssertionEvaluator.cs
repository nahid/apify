using APITester.Models;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace APITester.Services
{
    public class AssertionEvaluator
    {
        public TestResult EvaluateAssertion(TestAssertion assertion, ApiResponse response)
        {
            try
            {
                var assertionType = assertion.GetAssertionType();
                
                switch (assertionType)
                {
                    case AssertionType.StatusCode:
                        return EvaluateStatusCodeAssertion(assertion, response);
                    case AssertionType.ResponseBody:
                        return EvaluateResponseBodyAssertion(assertion, response);
                    case AssertionType.ResponseHeader:
                        return EvaluateResponseHeaderAssertion(assertion, response);
                    case AssertionType.ResponseTime:
                        return EvaluateResponseTimeAssertion(assertion, response);
                    default:
                        return TestResult.Failure(assertion.Name, "Unknown assertion type");
                }
            }
            catch (Exception ex)
            {
                return TestResult.Failure(assertion.Name, $"Error evaluating assertion: {ex.Message}");
            }
        }

        private TestResult EvaluateStatusCodeAssertion(TestAssertion assertion, ApiResponse response)
        {
            // Extract comparison type and value
            var assertionText = assertion.Assertion.Trim();
            
            // Handle range check (status >= 200 && status < 300)
            if (assertionText.Contains("&&"))
            {
                var parts = assertionText.Split("&&");
                if (parts.Length == 2)
                {
                    var firstPart = parts[0].Trim();
                    var secondPart = parts[1].Trim();
                    
                    var minMatch = Regex.Match(firstPart, @"status\s*(>=|>)\s*(\d+)");
                    var maxMatch = Regex.Match(secondPart, @"status\s*(<|<=)\s*(\d+)");
                    
                    if (minMatch.Success && maxMatch.Success)
                    {
                        var minOp = minMatch.Groups[1].Value;
                        var minValue = int.Parse(minMatch.Groups[2].Value);
                        var maxOp = maxMatch.Groups[1].Value;
                        var maxValue = int.Parse(maxMatch.Groups[2].Value);
                        
                        bool minCheck = minOp == ">=" ? response.StatusCode >= minValue : response.StatusCode > minValue;
                        bool maxCheck = maxOp == "<=" ? response.StatusCode <= maxValue : response.StatusCode < maxValue;
                        
                        if (minCheck && maxCheck)
                        {
                            return TestResult.Success(assertion.Name);
                        }
                        else
                        {
                            return TestResult.Failure(
                                assertion.Name,
                                $"Status code {response.StatusCode} is not in range {minOp} {minValue} && {maxOp} {maxValue}",
                                response.StatusCode.ToString(),
                                $"Range: {minOp} {minValue} && {maxOp} {maxValue}"
                            );
                        }
                    }
                }
            }
            
            // Handle simple equality check (status == 200)
            var match = Regex.Match(assertionText, @"status\s*(==|!=|>=|<=|>|<)\s*(\d+)");
            if (match.Success)
            {
                var op = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);
                
                bool passed = op switch
                {
                    "==" => response.StatusCode == value,
                    "!=" => response.StatusCode != value,
                    ">=" => response.StatusCode >= value,
                    "<=" => response.StatusCode <= value,
                    ">" => response.StatusCode > value,
                    "<" => response.StatusCode < value,
                    _ => false
                };
                
                if (passed)
                {
                    return TestResult.Success(assertion.Name);
                }
                else
                {
                    return TestResult.Failure(
                        assertion.Name,
                        $"Status code {response.StatusCode} {op} {value} assertion failed",
                        response.StatusCode.ToString(),
                        value.ToString()
                    );
                }
            }
            
            return TestResult.Failure(assertion.Name, $"Invalid status assertion format: {assertionText}");
        }

        private TestResult EvaluateResponseBodyAssertion(TestAssertion assertion, ApiResponse response)
        {
            var assertionText = assertion.Assertion.Trim();
            
            // Handle contains assertion (response.body contains "success")
            var containsMatch = Regex.Match(assertionText, @"response\.body\s+contains\s+(.+)");
            if (containsMatch.Success)
            {
                string expectedValue = containsMatch.Groups[1].Value.Trim('"', '\'');
                
                if (response.Body.Contains(expectedValue))
                {
                    return TestResult.Success(assertion.Name);
                }
                else
                {
                    return TestResult.Failure(
                        assertion.Name,
                        $"Response body does not contain '{expectedValue}'",
                        response.Body.Length > 100 ? response.Body.Substring(0, 100) + "..." : response.Body,
                        expectedValue
                    );
                }
            }
            
            // Handle JSON path expression (response.body.$.user.name == "John")
            var jsonPathMatch = Regex.Match(assertionText, @"response\.body\.\$\.([\w\.]+)\s*(==|!=|contains|startsWith|endsWith)\s*(.+)");
            if (jsonPathMatch.Success)
            {
                string jsonPath = jsonPathMatch.Groups[1].Value;
                string op = jsonPathMatch.Groups[2].Value;
                string expectedValue = jsonPathMatch.Groups[3].Value.Trim('"', '\'');
                
                try
                {
                    // Parse JSON
                    JObject jsonObj = JObject.Parse(response.Body);
                    
                    // Navigate JSON path and get value
                    string[] pathParts = jsonPath.Split('.');
                    JToken? currentToken = jsonObj;
                    
                    foreach (var part in pathParts)
                    {
                        currentToken = currentToken[part];
                        if (currentToken == null)
                        {
                            return TestResult.Failure(
                                assertion.Name,
                                $"JSON path '$.{jsonPath}' not found in response",
                                response.Body.Length > 100 ? response.Body.Substring(0, 100) + "..." : response.Body,
                                $"Path: $.{jsonPath}"
                            );
                        }
                    }
                    
                    string actualValue = currentToken.ToString();
                    
                    bool passed = op switch
                    {
                        "==" => actualValue == expectedValue,
                        "!=" => actualValue != expectedValue,
                        "contains" => actualValue.Contains(expectedValue),
                        "startsWith" => actualValue.StartsWith(expectedValue),
                        "endsWith" => actualValue.EndsWith(expectedValue),
                        _ => false
                    };
                    
                    if (passed)
                    {
                        return TestResult.Success(assertion.Name);
                    }
                    else
                    {
                        return TestResult.Failure(
                            assertion.Name,
                            $"JSON path '$.{jsonPath}' value '{actualValue}' {op} '{expectedValue}' assertion failed",
                            actualValue,
                            expectedValue
                        );
                    }
                }
                catch (JsonException ex)
                {
                    return TestResult.Failure(
                        assertion.Name,
                        $"Invalid JSON in response: {ex.Message}",
                        response.Body.Length > 100 ? response.Body.Substring(0, 100) + "..." : response.Body,
                        "Valid JSON expected"
                    );
                }
            }
            
            return TestResult.Failure(assertion.Name, $"Invalid body assertion format: {assertionText}");
        }

        private TestResult EvaluateResponseHeaderAssertion(TestAssertion assertion, ApiResponse response)
        {
            var assertionText = assertion.Assertion.Trim();
            
            // Handle header check (response.headers.Content-Type contains "application/json")
            var headerMatch = Regex.Match(assertionText, @"response\.headers\.([\w\-]+)\s*(==|!=|contains|startsWith|endsWith)\s*(.+)");
            if (headerMatch.Success)
            {
                string headerName = headerMatch.Groups[1].Value;
                string op = headerMatch.Groups[2].Value;
                string expectedValue = headerMatch.Groups[3].Value.Trim('"', '\'');
                
                string actualValue = response.GetHeader(headerName);
                
                bool passed = op switch
                {
                    "==" => actualValue == expectedValue,
                    "!=" => actualValue != expectedValue,
                    "contains" => actualValue.Contains(expectedValue),
                    "startsWith" => actualValue.StartsWith(expectedValue),
                    "endsWith" => actualValue.EndsWith(expectedValue),
                    _ => false
                };
                
                if (passed)
                {
                    return TestResult.Success(assertion.Name);
                }
                else
                {
                    return TestResult.Failure(
                        assertion.Name,
                        $"Header '{headerName}' value '{actualValue}' {op} '{expectedValue}' assertion failed",
                        actualValue,
                        expectedValue
                    );
                }
            }
            
            // Handle header existence check (response.headers contains "X-Powered-By")
            var headerExistsMatch = Regex.Match(assertionText, @"response\.headers\s+contains\s+(.+)");
            if (headerExistsMatch.Success)
            {
                string headerName = headerExistsMatch.Groups[1].Value.Trim('"', '\'');
                
                if (response.Headers.ContainsKey(headerName) || response.ContentHeaders.ContainsKey(headerName))
                {
                    return TestResult.Success(assertion.Name);
                }
                else
                {
                    return TestResult.Failure(
                        assertion.Name,
                        $"Response does not contain header '{headerName}'",
                        string.Join(", ", response.Headers.Keys.Concat(response.ContentHeaders.Keys)),
                        headerName
                    );
                }
            }
            
            return TestResult.Failure(assertion.Name, $"Invalid header assertion format: {assertionText}");
        }

        private TestResult EvaluateResponseTimeAssertion(TestAssertion assertion, ApiResponse response)
        {
            var assertionText = assertion.Assertion.Trim();
            
            // Handle response time check (response.time < 1000)
            var timeMatch = Regex.Match(assertionText, @"response\.time\s*(<|<=|>|>=|==|!=)\s*(\d+)");
            if (timeMatch.Success)
            {
                var op = timeMatch.Groups[1].Value;
                var value = int.Parse(timeMatch.Groups[2].Value);
                
                bool passed = op switch
                {
                    "<" => response.ResponseTimeMs < value,
                    "<=" => response.ResponseTimeMs <= value,
                    ">" => response.ResponseTimeMs > value,
                    ">=" => response.ResponseTimeMs >= value,
                    "==" => response.ResponseTimeMs == value,
                    "!=" => response.ResponseTimeMs != value,
                    _ => false
                };
                
                if (passed)
                {
                    return TestResult.Success(assertion.Name);
                }
                else
                {
                    return TestResult.Failure(
                        assertion.Name,
                        $"Response time {response.ResponseTimeMs}ms {op} {value}ms assertion failed",
                        response.ResponseTimeMs.ToString() + "ms",
                        value.ToString() + "ms"
                    );
                }
            }
            
            return TestResult.Failure(assertion.Name, $"Invalid response time assertion format: {assertionText}");
        }
    }
}
