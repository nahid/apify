using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Models
{
    public class MockApiDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = string.Empty;
        
        [JsonProperty("method")]
        public string Method { get; set; } = "GET";
        
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; } = 200;
        
        [JsonProperty("contentType")]
        public string ContentType { get; set; } = "application/json";
        
        [JsonProperty("headers")]
        public Dictionary<string, string>? Headers { get; set; }
        
        [JsonProperty("response")]
        public object? Response { get; set; }
        
        [JsonProperty("responseFile")]
        public string? ResponseFile { get; set; }
        
        [JsonProperty("delay")]
        public int Delay { get; set; } = 0;
        
        [JsonProperty("dynamic")]
        public bool IsDynamic { get; set; } = false;
        
        [JsonProperty("dynamicTemplate")]
        public string? DynamicTemplate { get; set; }
        
        [JsonProperty("matchQuery")]
        public Dictionary<string, string>? MatchQuery { get; set; }
        
        [JsonProperty("matchHeaders")]
        public Dictionary<string, string>? MatchHeaders { get; set; }
        
        // Allow conditions to match request parameters to different responses
        [JsonProperty("conditions")]
        public List<MockCondition>? Conditions { get; set; }
        
        public string GetResponseAsString()
        {
            if (Response == null)
                return string.Empty;
                
            if (Response is string responseString)
                return responseString;
                
            // Convert to JSON string if it's an object
            return JsonConvert.SerializeObject(Response, Formatting.Indented);
        }
    }
    
    public class MockCondition
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("queryParams")]
        public Dictionary<string, string>? QueryParams { get; set; }
        
        [JsonProperty("headers")]
        public Dictionary<string, string>? Headers { get; set; }
        
        [JsonProperty("body")]
        public object? Body { get; set; }
        
        [JsonProperty("response")]
        public object? Response { get; set; }
        
        [JsonProperty("statusCode")]
        public int? StatusCode { get; set; }
        
        [JsonProperty("responseFile")]
        public string? ResponseFile { get; set; }
    }
}