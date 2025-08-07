using Apify.Models;
using Apify.Utils;

namespace Apify.Services;

public class AssertionExecutor
{
    private readonly ResponseDefinitionSchema _responseDefinitionSchema;
    private readonly RequestDefinitionSchema _requestDefinitionSchema;
    private EnvironmentSchema? _environment;

    
    
    public AssertionExecutor(ResponseDefinitionSchema responseDefinitionSchema, RequestDefinitionSchema requestDefinitionSchema)
    {
        _responseDefinitionSchema = responseDefinitionSchema;
        _requestDefinitionSchema = requestDefinitionSchema;

    }

    private TestResults Run(List<AssertionEntity> assertions)
    {
        var scriptMan = new DynamicScriptingManager();
        var assertionTracker = new AssertionTracker();
        var testResults = new TestResults();
        
        scriptMan.SetVariable("assertionTracker", assertionTracker);
        
        if (_environment != null)
        {
            var envString = JsonHelper.SerializeWithEscapeSpecialChars(_environment.Variables); //JsonConvert.SerializeObject(_environment.Variables);
            var envExpr = $"JSON.parse(`{envString}`)";
            scriptMan.SetPropertyToAppObject("env", envExpr);
        }

        var jsonReq = JsonHelper.SerializeWithEscapeSpecialChars(_requestDefinitionSchema); 
        var jsonResp = JsonHelper.SerializeWithEscapeSpecialChars(_responseDefinitionSchema); 

        scriptMan.ExecuteScriptFromAssembly("Apify.includes.request.js");
        scriptMan.ExecuteScriptFromAssembly("Apify.includes.response.js");
        scriptMan.ExecuteScriptFromAssembly("Apify.includes.assert.js");
        var reqObjExpr = @"new Request(`" + jsonReq + @"`)";
        var respObjExpr = @"new Response(`" + jsonResp + @"`)";
        
        scriptMan.SetPropertyToAppObject("request", reqObjExpr);
        scriptMan.SetPropertyToAppObject("response", respObjExpr);
        scriptMan.SetPropertyToAppObject("assert", "new Assert()");

        
        foreach (var assertion in assertions)
        {
            bool testStatus;
            
            if (string.IsNullOrEmpty(assertion.Case))
            {
                throw new ArgumentException("Assertion expression cannot be null or empty.", nameof(assertion.Case));
            }

            try
            { 
                testStatus = scriptMan.Compile<bool>(assertion.Case);
            }
            catch (Exception ex)
            {
                testStatus = false;
                assertionTracker.AddResult(false, $"Error evaluating assertion > '{assertion.Title}': {ex.Message}");
            }
            
            var testResult = new TestResultEntity(assertion.Title, testStatus, assertionTracker.PopResults());
            testResults.AddResult(testResult);
        }

        return testResults;
    }

    public Task<TestResults> RunAsync(List<AssertionEntity> assertions, EnvironmentSchema? environment = null)
    {
        _environment = environment;
        
        return Task.FromResult(Run(assertions));
    }
    
}

public class Assert
{
    public AssertResponse? Response;
    private AssertionTracker _assertionTracker;
    
    
    public Assert(ResponseDefinitionSchema responseDefinitionSchema, AssertionTracker assertionTracker)
    {
        Response = new AssertResponse(responseDefinitionSchema, assertionTracker);
        _assertionTracker = assertionTracker ?? throw new ArgumentNullException(nameof(assertionTracker), "Assertion tracker cannot be null.");
    }
    
    public bool Equals(object? expected, object? actual, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Values are equal." : message;
        
        if (expected == null && actual == null)
        {
            return _assertionTracker.AddResult(true, message);
        }

        if (expected?.Equals(actual) ?? false)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Expected: {expected}, Actual: {actual}. {message}");
    }
    
    public bool NotEquals(object? expected, object? actual, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Values are not equal." : message;
        
        if (expected == null && actual == null)
        {
            return _assertionTracker.AddResult(false, "Both values are null. " + message);
        }

        if (!expected?.Equals(actual) ?? true)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Expected: {expected}, Actual: {actual}. {message}");
    }
    
    public bool IsTrue(bool condition, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Condition is true." : message;
        
        if (condition)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, "Condition is false. " + message);
    }
    
    public bool IsFalse(bool condition, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Condition is false." : message;
        
        if (!condition)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, "Condition is true. " + message);
    }
    
    public bool IsNull(object? value, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Value is null." : message;
        
        if (value == null)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Value is not null: {value}. " + message);
    }
    
    public bool IsNotNull(object? value, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Value is not null." : message;
        
        if (value != null)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, "Value is null. " + message);
    }
    
    public bool IsEmpty(string? value, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "String is empty." : message;
        
        if (string.IsNullOrEmpty(value))
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"String is not empty: '{value}'. " + message);
    }
    
    public bool IsNotEmpty(string? value, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "String is not empty." : message;
        
        if (!string.IsNullOrEmpty(value))
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, "String is empty. " + message);
    }
    
    public bool IsGreaterThan(int actual, int expected, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Actual value is greater than expected." : message;
        
        if (actual > expected)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: {actual}, Expected: {expected}. " + message);
    }
    
    public bool IsLessThan(int actual, int expected, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Actual value is less than expected." : message;
        
        if (actual < expected)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: {actual}, Expected: {expected}. " + message);
    }
    
    public bool IsGreaterThanOrEqual(int actual, int expected, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Actual value is greater than or equal to expected." : message;
        
        if (actual >= expected)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: {actual}, Expected: {expected}. " + message);
    }
    
    public bool IsLessThanOrEqual(int actual, int expected, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Actual value is less than or equal to expected." : message;
        
        if (actual <= expected)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: {actual}, Expected: {expected}. " + message);
    }
    
    public bool IsBetween(int actual, int lowerBound, int upperBound, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Actual value is between lower and upper bounds." : message;
        
        if (actual >= lowerBound && actual <= upperBound)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: {actual}, Lower Bound: {lowerBound}, Upper Bound: {upperBound}. " + message);
    }
    
    public bool IsNotBetween(int actual, int lowerBound, int upperBound, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "Actual value is not between lower and upper bounds." : message;
        
        if (actual < lowerBound || actual > upperBound)
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: {actual}, Lower Bound: {lowerBound}, Upper Bound: {upperBound}. " + message);
    }
    
    public bool Contains(object actual, object expected, string message = "")
    {
        message = string.IsNullOrEmpty(message) ? "String contains expected substring." : message;

        var heystack = actual.ToString();
        string? needle = expected.ToString();
        
        if (heystack != null && needle != null && heystack.Contains(needle))
        {
            return _assertionTracker.AddResult(true, message);
        }

        return _assertionTracker.AddResult(false, $"Actual: '{actual}', Expected Substring: '{expected}'. " + message);
    }
    
    
    
}

public class TestResultEntity
{
    public string Name { get; set; }
    public bool Status { get; set; }
    public List<AssertionResult> Result { get; set; }

    public TestResultEntity(string name, bool status, List<AssertionResult> assertionResult)
    {
        Name = name;
        Status = status;
        Result = assertionResult;
    }
}

public class TestResults
{
    public List<TestResultEntity> Results { get; set; } = new List<TestResultEntity>();

    public void AddResult(TestResultEntity result)
    {
        Results.Add(result);
    }
    public void AddResults(IEnumerable<TestResultEntity> results)
    {
        Results.AddRange(results);
    }

    public bool AllPassed => Results.All(r => r.Status);
    
    public int PassedCount => Results.Count(r => r.Status);
    
    public int FailedCount => Results.Count(r => !r.Status);
    
    public bool IsPassed()
    {
        return FailedCount == 0;
    }
}

public class AssertResponse
{
    private ResponseDefinitionSchema? _apiResponse;
    private AssertionTracker _assertionTracker;
    
    public AssertResponse(ResponseDefinitionSchema responseDefinitionSchema, AssertionTracker tracker)
    {
        _apiResponse = responseDefinitionSchema ?? throw new ArgumentNullException(nameof(responseDefinitionSchema), "API response cannot be null.");
        _assertionTracker = tracker ?? throw new ArgumentNullException(nameof(tracker), "Assertion tracker cannot be null.");
    }

    private string MakeMessage(string? message, string defaultMessage)
    {
        return string.IsNullOrEmpty(message) ? defaultMessage : message;
    }
    
    
    public bool StatusCodeIs(int statusCode, string message = "")
    {
        message = MakeMessage(message, "Status code matches " + statusCode + ".");
        
        if (_apiResponse == null)
        {
             return _assertionTracker.AddResult(false, "API response is null.");
        }

        if (_apiResponse.StatusCode != statusCode)
        {
            return _assertionTracker.AddResult(false, "API response status code does not match.");
        }

        return _assertionTracker.AddResult(true, message);
    }

    public bool StatusCodeIsNot(int statusCode, string message = "")
    {
        message = MakeMessage(message, "Status code does not match " + statusCode + ".");
        
        var resp = StatusCodeIs(statusCode, message);
        
        if (!resp)
        {
            return _assertionTracker.AddResult(false, "Status code should not be " + statusCode + ", but it was.");
        }
        
        return _assertionTracker.AddResult(true, message);
    }
    
    public bool HasHeader(string headerName, string message = "")
    {
        message = MakeMessage(message, "Header '" + headerName + "' exists in the response.");
        if (_apiResponse == null)
        {
            return _assertionTracker.AddResult(false, "API response is null.");
        }

        if (!_apiResponse.Headers.ContainsKey(headerName))
        {
            return _assertionTracker.AddResult(false, "Header '" + headerName + "' does not exist in the response.");
        }

        return _assertionTracker.AddResult(true, message);
    }
    
    public bool HeaderValueContains(string headerName, string expectedSubstring, string message = "")
    {
        message = MakeMessage(message, $"Header '{headerName}' contains '{expectedSubstring}'.");
        if (_apiResponse == null)
            return _assertionTracker.AddResult(false, "API response is null.");

        if (!_apiResponse.Headers.TryGetValue(headerName, out var value) ||
            !value.Contains(expectedSubstring))
        {
            return _assertionTracker.AddResult(false,
                $"Header '{headerName}' was '{value ?? "<missing>"}', " +
                $"which does not contain '{expectedSubstring}'.");
        }

        return _assertionTracker.AddResult(true, message);
    }
    
    public bool ContentTypeIs(string contentType, string message = "")
    {
        message = MakeMessage(message, "Content-Type matches " + contentType + ".");
        
        if (_apiResponse == null)
        {
            return _assertionTracker.AddResult(false, "API response is null.");
        }

        if (!_apiResponse.Headers.TryGetValue("Content-Type", out var actualContentType) || actualContentType != contentType)
        {
            return _assertionTracker.AddResult(false, "Content-Type does not match.");
        }

        return _assertionTracker.AddResult(true, message);
    }
    
    public bool BodyContains(string searchString, string message = "")
    {
        message = MakeMessage(message, "Response body contains '" + searchString + "'.");
        
        if (_apiResponse == null)
        {
            return _assertionTracker.AddResult(false, "API response is null.");
        }
    
        if (string.IsNullOrEmpty(_apiResponse.Body) || !_apiResponse.Body.Contains(searchString))
        {
            return _assertionTracker.AddResult(false, "Response body does not contain '" + searchString + "'.");
        }
    
        return _assertionTracker.AddResult(true, message);
    }
    
    public bool BodyMatchesRegex(string pattern, string message = "")
    {
        message = MakeMessage(message, "Response body matches the pattern '" + pattern + "'.");
    
        if (_apiResponse == null)
        {
            return _assertionTracker.AddResult(false, "API response is null.");
        }
    
        if (string.IsNullOrEmpty(_apiResponse.Body) || !System.Text.RegularExpressions.Regex.IsMatch(_apiResponse.Body, pattern))
        {
            return _assertionTracker.AddResult(false, "Response body does not match the pattern '" + pattern + "'.");
        }
    
        return _assertionTracker.AddResult(true, message);
    }
    
    public bool RedirectsTo(string expectedLocation, string message = "")
    {
        message = MakeMessage(message, $"Response redirects to '{expectedLocation}'.");
        if (_apiResponse == null)
            return _assertionTracker.AddResult(false, "API response is null.");

        int code = _apiResponse.StatusCode;
        if (code < 300 || code >= 400)
            return _assertionTracker.AddResult(false,
                $"Status code {code} is not a redirect (3xx).");

        if (!_apiResponse.Headers.TryGetValue("Location", out var loc) ||
            !string.Equals(loc, expectedLocation, StringComparison.Ordinal))
        {
            return _assertionTracker.AddResult(false,
                $"Location header was '{loc ?? "<missing>"}', " +
                $"expected '{expectedLocation}'.");
        }

        return _assertionTracker.AddResult(true, message);
    }
    
    public bool IsRedirected(string message = "")
    {
        message = MakeMessage(message, "Response is a redirect.");
        
        if (_apiResponse == null)
        {
            return _assertionTracker.AddResult(false, "API response is null.");
        }

        int code = _apiResponse.StatusCode;
        if (code < 300 || code >= 400)
        {
            return _assertionTracker.AddResult(false, "Status code " + code + " is not a redirect (3xx).");
        }

        return _assertionTracker.AddResult(true, message);
    }

    
}