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

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("RequestOptions")]
        public ApiCallDisplayOptions? RequestOptions { get; set; } = new ApiCallDisplayOptions();

        [JsonProperty("Environments")]
        public List<EnvironmentSchema> Environments { get; set; }
        
        [JsonProperty("Authorization")]
        public Authorization? Authorization { get; set; }

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
        
        [JsonProperty("Variables")]
        public Dictionary<string, string> Variables { get; set; }
        
        [JsonProperty("MockServer")]
        public MockServer? MockServer { get; set; }
        
    }

    public class Authorization
    {
        [JsonProperty("Type")]
        public string? Type { get; set; }
        
        [JsonProperty("Token")]
        public string? Token { get; set; }
    }

    public class ApiCallDisplayOptions
    {
        [JsonProperty("Verbose")]
        public bool? Verbose { get; set; }
        
        [JsonProperty("Tests")]
        public bool? Tests { get; set; }
        
        [JsonProperty("ShowRequest")]
        public bool? ShowRequest { get; set; }
        
        [JsonProperty("ShowResponse")]
        public bool? ShowResponse { get; set; }
        
        [JsonProperty("ShowOnlyResponse")]
        public bool? ShowOnlyResponse { get; set; }
    }


    public class MockServer
    {
        [JsonProperty("Port")]
        public int Port { get; set; }
        
        [JsonProperty("Verbose")]
        public bool Verbose { get; set; }
        
        [JsonProperty("EnableCors")]
        public bool EnableCors { get; set; }
        
        [JsonProperty("DefaultHeaders")]
        public Dictionary<string, string>? DefaultHeaders { get; set; }
    }
}