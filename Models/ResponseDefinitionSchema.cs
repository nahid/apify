using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Models;


public class ResponseDefinitionSchema
{
    [JsonProperty("isSuccessful")]
    public bool IsSuccessful { get; set; }
    
    [JsonProperty("statusCode")]
    public int StatusCode { get; set; }
    
    [JsonProperty("headers")]
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    
    [JsonProperty("contentHeaders")]
    public Dictionary<string, string> ContentHeaders { get; set; } = new Dictionary<string, string>();
    
    [JsonProperty("contentType")]
    public string? ContentType { get; set; }
    
    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;
    
    
    [JsonProperty("json")]
    public JToken? Json { get; set; }
    
    [JsonProperty("responseTimeMs")]
    public long ResponseTimeMs { get; set; }
    
    [JsonProperty("errorMessage")]
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