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

        [JsonProperty("Environments")]
        public List<EnvironmentSchema> Environments { get; set; }

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
        
        [JsonProperty("Variables")]
        public Dictionary<string, string> Variables { get; set; }
        
        [JsonProperty("MockServer")]
        public MockServer? MockServer { get; set; } = null;
        
    }


    public class MockServer
    {
        [JsonProperty("Port")]
        public int Port { get; set; } = 0;
        
        [JsonProperty("Verbose")]
        public bool Verbose { get; set; } = false;
        
        [JsonProperty("DefaultHeaders")]
        public Dictionary<string, string>? DefaultHeaders { get; set; } = null;
    }
}