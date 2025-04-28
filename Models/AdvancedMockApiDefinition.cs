using System.Collections.Generic;
using Newtonsoft.Json;
using Apify.Utils;

namespace Apify.Models
{
    /// <summary>
    /// Represents an advanced mock API definition with improved condition handling
    /// </summary>
    public class AdvancedMockApiDefinition
    {
        /// <summary>
        /// Name of the API endpoint
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what this mock API does
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// HTTP method (GET, POST, PUT, DELETE, etc.)
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; } = "GET";
        
        /// <summary>
        /// The endpoint path, can include route parameters (e.g., /users/:id)
        /// </summary>
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = string.Empty;
        
        /// <summary>
        /// List of possible responses based on conditions
        /// </summary>
        [JsonProperty("responses")]
        public List<ConditionalResponse> Responses { get; set; } = new List<ConditionalResponse>();
    }
    
    /// <summary>
    /// Represents a mock response with condition
    /// </summary>
    public class ConditionalResponse
    {
        /// <summary>
        /// Expression that determines if this response should be used.
        /// Can access `headers`, `body`, `query`, and `params` variables.
        /// Use "default" for the fallback response.
        /// </summary>
        [JsonProperty("condition")]
        public string Condition { get; set; } = string.Empty;
        
        /// <summary>
        /// HTTP status code to return
        /// </summary>
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; } = 200;
        
        /// <summary>
        /// HTTP headers to include in the response
        /// </summary>
        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Response body template - can be an object or array
        /// </summary>
        [JsonProperty("responseTemplate")]
        [JsonConverter(typeof(ObjectArrayJsonConverter))]
        public object ResponseTemplate { get; set; } = new { };
    }
}