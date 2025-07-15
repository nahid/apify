using Newtonsoft.Json.Linq;

namespace Apify.Models;


public class ApiResponse
{
    public bool IsSuccessful { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> ContentHeaders { get; set; } = new Dictionary<string, string>();
    public string? ContentType { get; set; } = null;
    public string Body { get; set; } = string.Empty;
    public JToken? Json { get; set; } = null;
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }

    public string GetHeader(string name)
    {
        if (Headers.TryGetValue(name, out string? value))
            return value;
            
        if (ContentHeaders.TryGetValue(name, out string? contentValue))
            return contentValue;
            
        return string.Empty;
    }

    public bool HasHeader(string name)
    {
        return Headers.ContainsKey(name) || ContentHeaders.ContainsKey(name);
    }
        
        
}