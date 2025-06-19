using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Apify.Models
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
            Environments = new List<EnvironmentSchema>();
            DefaultEnvironment = "Development";
        }

        // Parameterized constructor for creating profiles programmatically
        public ConfigurationProfile(string name, string? description, List<EnvironmentSchema> environments, string? defaultEnvironment)
        {
            Name = name;
            Description = description;
            Environments = environments ?? new List<EnvironmentSchema>();
            DefaultEnvironment = defaultEnvironment;
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("Environments")]
        public List<EnvironmentSchema> Environments { get; set; }

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
    }
}