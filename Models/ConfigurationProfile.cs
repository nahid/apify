using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class ConfigurationProfile
    {
        // This constructor is needed for Native AOT compatibility with JSON deserialization
        [JsonConstructor]
        public ConfigurationProfile()
        {
            Name = string.Empty;
            Environments = new List<TestEnvironment>();
            DefaultEnvironment = "Development";
        }

        // Parameterized constructor for creating profiles programmatically
        public ConfigurationProfile(string name, string? description, List<TestEnvironment> environments, string? defaultEnvironment)
        {
            Name = name;
            Description = description;
            Environments = environments ?? new List<TestEnvironment>();
            DefaultEnvironment = defaultEnvironment;
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("Environments")]
        public List<TestEnvironment> Environments { get; set; }

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
    }
}