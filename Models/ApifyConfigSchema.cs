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
            Name = name;
            Description = description;
            Environments = environments;
            DefaultEnvironment = defaultEnvironment ?? "Development";
            Variables = variables ?? new Dictionary<string, string>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("requestOptions")]
        public ApiCallDisplayOptions? RequestOptions { get; set; } = new ApiCallDisplayOptions();

        [JsonProperty("environments")]
        public List<EnvironmentSchema> Environments { get; set; }
        
        [JsonProperty("authorization")]
        public AuthorizationSchema? Authorization { get; set; }

        [JsonProperty("defaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
        
        [JsonProperty("variables")]
        public Dictionary<string, string> Variables { get; set; }
        
        [JsonProperty("mockServer")]
        public MockServer? MockServer { get; set; }
        
    }

    public class ApiCallDisplayOptions
    {
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }
        
        [JsonProperty("tests")]
        public bool? Tests { get; set; }
        
        [JsonProperty("showRequest")]
        public bool? ShowRequest { get; set; }
        
        [JsonProperty("showResponse")]
        public bool? ShowResponse { get; set; }
        
        [JsonProperty("showOnlyResponse")]
        public bool? ShowOnlyResponse { get; set; }
    }


    public class MockServer
    {
        [JsonProperty("port")]
        public int Port { get; set; }
        
        [JsonProperty("verbose")]
        public bool Verbose { get; set; }
        
        [JsonProperty("enableCors")]
        public bool EnableCors { get; set; }
        
        [JsonProperty("defaultHeaders")]
        public Dictionary<string, string>? DefaultHeaders { get; set; }
    }
}