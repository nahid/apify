using Apify.Models;
using DynamicExpresso;

namespace Apify.Services;

public class AssertionExecutor
{
    private ApiResponse _apiResponse;

    
    
    public AssertionExecutor(ApiResponse apiResponse)
    {
        _apiResponse = apiResponse;

    }

    public List<TestResult> Run(List<AssertionEntity> assertions)
    {
        var interpreter = new Interpreter();
        var assertionTracker = new AssertionTracker();
        var assertionResults = new List<TestResult>();
        
        interpreter.SetVariable("Assert", new Assert(_apiResponse, assertionTracker));
        
        foreach (var assertion in assertions)
        {
            bool testStatus;
            if (string.IsNullOrEmpty(assertion.Case))
            {
                throw new ArgumentException("Assertion expression cannot be null or empty.", nameof(assertion.Case));
            }

            try
            { 
                testStatus = interpreter.Eval<bool>(assertion.Case);
            }
            catch (Exception ex)
            {
                testStatus = false;
                assertionTracker.AddResult(false, $"Error evaluating assertion '{assertion.Title}': {ex.Message}");
            }

         

            var testResult = new TestResult(assertion.Title, testStatus, assertionTracker.PopResults());
            assertionResults.Add(testResult);
        }

        return assertionResults;
    }
    
}

public class Assert
{
    public AssertResponse? Response;
    private AssertionTracker _assertionTracker;
    
    
    public Assert(ApiResponse apiResponse, AssertionTracker assertionTracker)
    {
        Response = new AssertResponse(apiResponse, assertionTracker);
    }
}

public class TestResult
{
    public string Name { get; set; } = string.Empty;
    public bool Status { get; set; }
    public List<AssertionResult> Result { get; set; }

    public TestResult(string name, bool status, List<AssertionResult> assertionResult)
    {
        Name = name;
        Status = status;
        Result = assertionResult;
    }
}

public class AssertResponse
{
    private ApiResponse? _apiResponse;
    private AssertionTracker _assertionTracker;
    
    public AssertResponse(ApiResponse apiResponse, AssertionTracker tracker)
    {
        _apiResponse = apiResponse ?? throw new ArgumentNullException(nameof(apiResponse), "API response cannot be null.");
        _assertionTracker = tracker ?? throw new ArgumentNullException(nameof(tracker), "Assertion tracker cannot be null.");
    }

    private string MakeMessage(string? message, string defaultMessage)
    {
        return string.IsNullOrEmpty(message) ? defaultMessage : message;
    }
    
    public bool IsStatusCode(int statusCode, string message = "")
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
        
        var resp = IsStatusCode(statusCode, message);
        
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
    
    
    
}