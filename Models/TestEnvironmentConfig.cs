using System.Collections.Generic;
using Newtonsoft.Json;

namespace APITester.Models
{
    public class TestEnvironmentConfig
    {
        // Default constructor with default values
        public TestEnvironmentConfig()
        {
            Name = "Default";
            Environments = new List<TestEnvironment>();
            DefaultEnvironment = "Development";
            Variables = new Dictionary<string, string>();
        }

        // Parameterized constructor for programmatic creation
        public TestEnvironmentConfig(string name, string? description, List<TestEnvironment> environments, string? defaultEnvironment, Dictionary<string, string>? variables = null)
        {
            Name = name ?? "Default";
            Description = description;
            Environments = environments ?? new List<TestEnvironment>();
            DefaultEnvironment = defaultEnvironment ?? "Development";
            Variables = variables ?? new Dictionary<string, string>();
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("Environments")]
        public List<TestEnvironment> Environments { get; set; }

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
        
        [JsonProperty("Variables")]
        public Dictionary<string, string> Variables { get; set; }
    }
}