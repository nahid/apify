namespace Apify.Models;

public class AssertionTracker
{
    private readonly List<AssertionResult> _results = new List<AssertionResult>();

    public bool AddResult(bool status, string message = "Assertion passed")
    {
        var result = new AssertionResult(status, message);
        _results.Add(result);

        return status;
    }
    
    public List<AssertionResult> GetResults()
    {
        return _results;
    }
    
    public List<AssertionResult> PopResults()
    {
        var results = new List<AssertionResult>(_results);
        _results.Clear();
        
        return results;
    }
    


}

public class AssertionResult
{
    private bool _status;
    private string _message;
    public AssertionResult(bool status, string message = "")
    {
        _status = status;
        _message = message;
    }

    public bool IsPassed()
    {
        return _status;
    }
    
    public bool IsFailed()
    {
        return !_status;
    }
    
    public string GetMessage()
    {
        return _message;
    }
}