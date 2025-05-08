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
        
        [JsonProperty("acceptsFileUpload")]
        public bool AcceptsFileUpload { get; set; } = false;
        
        [JsonProperty("fileFieldName")]
        public string FileFieldName { get; set; } = "file";
        
        [JsonProperty("saveUploadedFilesTo")]
        public string? SaveUploadedFilesTo { get; set; }
        
        [JsonProperty("requireAuthentication")]
        public bool RequireAuthentication { get; set; } = false;
        
        [JsonProperty("authHeaderName")]
        public string AuthHeaderName { get; set; } = "Authorization";
        
        [JsonProperty("authHeaderPrefix")]
        public string AuthHeaderPrefix { get; set; } = "Bearer";
        
        [JsonProperty("validTokens")]
        public List<string>? ValidTokens { get; set; }
        
        [JsonProperty("unauthorizedResponse")]
        public object? UnauthorizedResponse { get; set; }
        
        // Dynamic data generation options
        [JsonProperty("generateDynamicData")]
        public bool GenerateDynamicData { get; set; } = false;
        
        [JsonProperty("dynamicDataCount")]
        public int DynamicDataCount { get; set; } = 10;
        
        [JsonProperty("dynamicDataTemplate")]
        public object? DynamicDataTemplate { get; set; }
        
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
        
        [JsonProperty("headersContain")]
        public Dictionary<string, string>? HeadersContain { get; set; }
        
        [JsonProperty("headerExists")]
        public List<string>? HeaderExists { get; set; }
        
        [JsonProperty("body")]
        public object? Body { get; set; }
        
        [JsonProperty("bodyContains")]
        public List<string>? BodyContains { get; set; }
        
        [JsonProperty("bodyMatches")]
        public Dictionary<string, string>? BodyMatches { get; set; }
        
        [JsonProperty("response")]
        public object? Response { get; set; }
        
        [JsonProperty("statusCode")]
        public int? StatusCode { get; set; }
        
        [JsonProperty("responseFile")]
        public string? ResponseFile { get; set; }
        
        [JsonProperty("dynamicTemplate")]
        public string? DynamicTemplate { get; set; }
    }
}