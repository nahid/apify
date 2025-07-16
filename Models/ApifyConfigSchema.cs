using System.Collections.Generic;
using Newtonsoft.Json;

namespace Apify.Models
{
    public class ApifyConfigSchema
    {
        // Default constructor with default values
        public ApifyConfigSchema()
        {
            Name = "Default";
            Environments = new List<EnvironmentSchema>();
            DefaultEnvironment = "Development";
            Variables = new Dictionary<string, string>();
        }

        // Parameterized constructor for programmatic creation
        public ApifyConfigSchema(string name, string? description, List<EnvironmentSchema> environments, string? defaultEnvironment, Dictionary<string, string>? variables = null)
        {
            Name = name ?? "Default";
            Description = description;
            Environments = environments ?? new List<EnvironmentSchema>();
            DefaultEnvironment = defaultEnvironment ?? "Development";
            Variables = variables ?? new Dictionary<string, string>();
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("RequestOptions")]
        public ApiCallDisplayOptions? RequestOptions { get; set; } = new ApiCallDisplayOptions();

        [JsonProperty("Environments")]
        public List<EnvironmentSchema> Environments { get; set; }
        
        [JsonProperty("Authorization")]
        public Authorization? Authorization { get; set; } = null;

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
        
        [JsonProperty("Variables")]
        public Dictionary<string, string> Variables { get; set; }
        
        [JsonProperty("MockServer")]
        public MockServer? MockServer { get; set; } = null;
        
    }

    public class Authorization
    {
        [JsonProperty("Type")]
        public string Type { get; set; }
        
        [JsonProperty("Token")]
        public string Token { get; set; }
    }

    public class ApiCallDisplayOptions
    {
        [JsonProperty("Verbose")]
        public bool? Verbose { get; set; } = null;
        
        [JsonProperty("Tests")]
        public bool? Tests { get; set; } = null;
        
        [JsonProperty("ShowRequest")]
        public bool? ShowRequest { get; set; } = null;
        
        [JsonProperty("ShowResponse")]
        public bool? ShowResponse { get; set; } = null;
        
        [JsonProperty("ShowOnlyResponse")]
        public bool? ShowOnlyResponse { get; set; } = null;
    }


    public class MockServer
    {
        [JsonProperty("Port")]
        public int Port { get; set; } = 0;
        
        [JsonProperty("Verbose")]
        public bool Verbose { get; set; } = false; 
        
        [JsonProperty("EnableCors")]
        public bool EnableCors { get; set; } = false;
        
        [JsonProperty("DefaultHeaders")]
        public Dictionary<string, string>? DefaultHeaders { get; set; } = null;
    }
}